---
slug: blazor-goodbye-js
title: Say goodbye to JavaScript in your Blazor web app
authors: trungnt2910
tags: [C#, JS, .NET, Blazor]
---

Uni is taking too much of my time, and so I cannot work much on Project Reality and `lxmonika`.
However, while building a Blazor web app as part of my course, I do have a .NET/JS interop trick
I want to share!

<!-- truncate -->

## Prerequisites

- JavaScript and [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor) knowledge.
- [Interop](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability)
between .NET/Blazor and JavaScript.

## The problem

The most annoying issue when working with Blazor is probably times when you need to call a bit of
JavaScript. While HTML elements and, to some extent, CSS styles (with `Element.style`), can be
defined inline in Razor components, running bits of JavaScript is not easy.

The [generally accepted way](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/location-of-javascript)
is to create a separate file for your JavaScript and declare them all in the main `App.razor` page
as `<script>` tags. The custom JavaScript needs to expose a few functions to the global namespace,
allowing them to be called using [`IJSRuntime.InvokeAsync`](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet).

This is especially troublesome when you sometimes have to do a few simple actions not natively
supported by Blazor, such as [clicking a button](https://stackoverflow.com/questions/69042684/is-there-a-blazor-way-of-simulating-a-click-event-on-an-html-element)
(`button.click()`) or submitting a form (`form.submit()`). Using the widely documented approach,
one would need to create a whole new JavaScript file, declare a new `<script>` tag, and pollute the
global namespace for what could have been a few JavaScript one-liners.

## Some background

### `IJSRuntime` internals

Before going further, let's see what this `IJSRuntime` does under the hood.

When `IJSRuntime.InvokeAsync` reaches the browser's JavaScript runtime, it splits the function
"[`identifier`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsruntime.invokeasync?view=aspnetcore-8.0#microsoft-jsinterop-ijsruntime-invokeasync-1(system-string-system-object()))"
by the `.` symbol. Then, it starts accessing members, starting from the `window` object:

(Extracted from [`Microsoft.JSInterop.ts`](https://github.com/dotnet/aspnetcore/blob/7033ec7f402fa4b7df2bf72a5e3dff1df6831f57/src/JSInterop/Microsoft.JSInterop.JS/src/src/Microsoft.JSInterop.ts#L24-L68)):
```ts
  class JSObject {
      // [some parts omitted]

      public findFunction(identifier: string) {
          // [some parts omitted]

          let result: any = this._jsObject;
          let lastSegmentValue: any;

          identifier.split(".").forEach(segment => {
              if (segment in result) {
                  lastSegmentValue = result;
                  result = result[segment];
              } else {
                  throw new Error(`Could not find '${identifier}' ('${segment}' was undefined).`);
              }
          });

          if (result instanceof Function) {
              result = result.bind(lastSegmentValue);
              this._cachedFunctions.set(identifier, result);
              return result;
          }

          throw new Error(`The value '${identifier}' is not a function.`);
      }

          // [some parts omitted]
  }

  const windowJSObjectId = 0;
  const cachedJSObjectsById: { [id: number]: JSObject } = {
      [windowJSObjectId]: new JSObject(window)
  };
```

Why does it start looking from the `window` object, you may ask? Let's move on to the next section.

### Global symbols and `window`

In browser JavaScript, the global scope is the same as the `window` object. Everything accessible
globally can also be accessed through `window.`. For example,

```js
var foo = 2910;
console.log(foo);
console.log(window.foo);
```

should output:

```
2910
2910
```

This is not restricted to variables or user-defined symbols. Built-in classes and functions can also
be referenced this way:

```js
window.JSON
```

outputs (at least, on Microsoft Edge):

```js
JSON {Symbol(Symbol.toStringTag): 'JSON', parse: ƒ, stringify: ƒ, rawJSON: ƒ, isRawJSON: ƒ}
```

### Conditions for functions passed to `IJSRuntime.InvokeAsync`

Knowing the search strategy and source code, we can determine the conditions of the JavaScript
expression passed to `IJSRuntime.InvokeAsync` method:
- The expression must be a **function** (otherwise, `result instanceof Function` would fail).
- The expression must be a **chain of attributes, separated by a `"."`**. These examples would fail:
    + `window["alert"]`: Despite returning the same function, `JSObject` cannot look this up since
    it would translate to an attempt to look for a member named `"window[\"alert\"]"` in `window`.
    + `(function(a) {return window.alert(a)})`: `IJSRuntime` and its underlying helpers do not use
    any kind of `eval`, and therefore cannot parse this function. The TypeScript code would
    decompose that to something similar to
    `window["(function(a) {return window"]["alert(a)})"]`.
- **No primitives** may appear in this chain of attributes.
    This expression would fail: `document.title.valueOf`:
    ```
    Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: Cannot use 'in' operator to search for 'valueOf' in Home
      TypeError: Cannot use 'in' operator to search for 'valueOf' in Home
    ```
    "Home" here is actually the primitive string that `document.title` is currently holding for the
    test page. What failed here was the `if (segment in result)` check, where `result` is currently
    `document.title` (or `"Home"`), and `segment` is `"valueOf"`.
    The same applies for other primitives like `number`
    (try `document.body.childNodes.length.valueOf`) or `boolean`.

    There will be more discussion about `valueOf` in the sections below.

## The solution

### Functions

Let's go back to the [button click example](https://stackoverflow.com/questions/69042684/is-there-a-blazor-way-of-simulating-a-click-event-on-an-html-element).
We have a reference to a button (probably as a `ElementReference`), and we now need to call `click`
on it.

We cannot directly invoke JavaScript methods on `ElementReference`, but `IJSRuntime` can marshal it
to the corresponding JavaScript object. We also know that the HTML `<button>` has a corresponding
JavaScript class called
[`HTMLButtonElement`](https://developer.mozilla.org/en-US/docs/Web/API/HTMLButtonElement).

From this, we can easily get the `click` method from the prototype:

```js
window.HTMLButtonElement.prototype.click
```

As a member of `prototype`, the function is bound to the prototype object. To invoke it with the
button reference we have as the `this` argument, we can use
[`.call`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function/call):

This gives us:
```js
window.HTMLButtonElement.prototype.click.call
```

All we need now is to pass the button reference we have to the function above.
We can try it in Blazor:
```razor
@inject IJSRuntime JSRuntime

<button @onclick="Button1_Click">Click me</button>
<button @ref="_button2" @onclick="Button2_Click" style="display: none;"></button>
<p>Counter value: @_value</p>

@code {
    private ElementReference _button1;
    private ElementReference _button2;
    private int _value = 0;

    private async Task Button1_Click()
    {
        await JSRuntime.InvokeVoidAsync(
            "window.HTMLButtonElement.prototype.click.call", _button2);
    }

    private void Button2_Click()
    {
        ++_value;
        StateHasChanged();
    }
}
```

### Attributes/Properties

The [`Reflect`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect)
JavaScript global object contains a few useful static functions for manipulating JS objects.

#### Getting the `window` object

Before being able to access anything, we would need to grab a reference to `window`, or a similar
global object like `document`. For most JavaScript objects, we can use
[`valueOf`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object/valueOf)
to grab itself:

```cs
await using var window = await JSRuntime.InvokeAsync<IJSObjectReference>("window.valueOf");
```

Or maybe just:

```cs
await using var window = await JSRuntime.InvokeAsync<IJSObjectReference>("valueOf");
```

A similar trick can be done for other global objects like `document`.

#### Getting

To get properties, first, acquire a reference. Then, we can call
[`Reflect.get`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/get)
on that reference:

```js
let ref = document.valueOf();
let title = Reflect.get(ref, "title");
console.log(title);
```

Or, in Blazor:

```razor
@inject IJSRuntime JSRuntime

<button @onclick="Button_Click">Click me</button>
<p>Title: @_title</p>

@code {
    private string? _title;

    private async Task Button_Click()
    {
        await using var document = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "document.valueOf"
        );
        _title = await JSRuntime.InvokeAsync<string>("Reflect.get", document, "title");
        StateHasChanged();
    }
}
```

Another way to get values is using `valueOf`, just like what we did for `window` and `document`.
This has an advantage of saving us from creating an extra object reference. However, as discussed
above, `valueOf` will not work with primitive types as .NET tries to look for that member using the
`in` operator. Furthermore, `valueOf` may be overriden by classes to return something other than
itself.

#### Setting

To set properties, we can use
[`Reflect.set`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/set)
on a reference obtained by the same way:

```js
let ref = document.valueOf();
Reflect.set(ref, "title", "Just Monika!");
```

In Blazor:

```razor
@inject IJSRuntime JSRuntime

<button @onclick="Button_Click">Click me</button>

@code {
    private int _value = 0;

    private async Task Button_Click()
    {
        ++_value;
        await using var document = await JSRuntime.InvokeAsync<IJSObjectReference>(
            "document.valueOf"
        );
        await JSRuntime.InvokeVoidAsync("Reflect.set", document, "title",
            $"Just Monika | Counter value: {_value}");
    }
}
```

### Constructors

Now that we can manipulate all objects that we can get our hands on, the final task we need to do
before getting rid of JavaScript completely is constructing our own objects from C#. In other words,
we need to find a way to invoke constructors.

#### Non-`class` objects

Some JavaScript constructors are accessible through `<ClassName>.prototype.constructor`, or directly
using `<ClassName>` itself:

```js
// Boxes this primitive
Object(2910);
// This also works
Object.prototype.constructor(2910);
```

Output:

```
Number {2910}
Number {2910}
```

#### `class` objects

However, [others aren't](https://stackoverflow.com/questions/30689817/es6-call-class-constructor-without-new-keyword):

```
Set.prototype.constructor("lxmonika");
```

Output:

```
Uncaught TypeError: Constructor Set requires 'new'
    at Set (<anonymous>)
    at <anonymous>:1:15
```

These are JavaScript objects declared as
[ES6 `class`es](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Classes).


#### Attempt - `Reflect.construct`

To work around this `new` keyword requirement, we might be able to use the static function
[`Reflect.construct`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/construct):

For these, we first fetch a function reference, then wrap our args into a JS array, then call
`Reflect.construct` on that:

```js
let ctor = Set.bind();
let args = Array("lxmonika")
Reflect.construct(ctor, args);
```

In Blazor, it can be something like:

```razor
@inject IJSRuntime JSRuntime

<button @onclick="Button_Click">Click me</button>

@code {
    private async Task Button_Click()
    {
        await using var ctor = await JSRuntime.InvokeAsync<IJSObjectReference>("Set.bind");
        await using var set = await JSRuntime.InvokeAsync<IJSObjectReference>("Reflect.construct",
            new object?[] { ctor, new string[] { "lxmonika" } });
        await JSRuntime.InvokeVoidAsync("console.log", set);
    }

}
```

Looks perfect, doesn't it? But no:

```
Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: Cannot create a JSObjectReference from the value 'function () { [native code] }'.
      Error: Cannot create a JSObjectReference from the value 'function () { [native code] }'.
```

Microsoft has an
[artificial restriction](https://github.com/dotnet/aspnetcore/blob/7033ec7f402fa4b7df2bf72a5e3dff1df6831f57/src/JSInterop/Microsoft.JSInterop.JS/src/src/Microsoft.JSInterop.ts#L148)
preventing `function`s from being bound to a `JSObjectReference`:

```ts
  /**
   * Creates a JavaScript object reference that can be passed to .NET via interop calls.
   *
   * @param jsObject The JavaScript Object used to create the JavaScript object reference.
   * @returns The JavaScript object reference (this will be the same instance as the given object).
   * @throws Error if the given value is not an Object.
   */
  export function createJSObjectReference(jsObject: any): any {
      if (jsObject && typeof jsObject === "object") {
          cachedJSObjectsById[nextJsObjectId] = new JSObject(jsObject);

          const result = {
              [jsObjectIdKey]: nextJsObjectId
          };

          nextJsObjectId++;

          return result;
      }

      throw new Error(`Cannot create a JSObjectReference from the value '${jsObject}'.`);
  }
```

I have started a discussion with Microsoft
[here](https://github.com/dotnet/aspnetcore/discussions/55259); hopefully they can come with a
clever solution for this.

#### `eval`

We have managed to avoid `eval` all the way until here, both because of its bad reputation and the
fact that many pages have content policies preventing code from accessing that function. `eval` is
so powerful that it alone could have solved all our method call/property get/property set issues
above, and it can also call constructors for us:

```cs
await using var set = await JSRuntime.InvokeAsync<IJSObjectReference>(
    "window.eval", @"new Set(""lxmonika"")"
);
```

#### Wrapper functions

If you cannot use `eval` at all costs, then the only solution left is a wrapper function:

```js
function constructByName(name, ...args) {
    return Reflect.construct(window[name], args);
}
constructByName("Set", "lxmonika");
```

But seriously, do we ever need a JavaScript `Set` that bad from Blazor code?

## Future .NET

ASP.NET Core has an [issue](https://github.com/dotnet/aspnetcore/issues/31151) open for accessing
constructors, invoking callbacks, and getting/setting properties from Blazor code.

It has been open since the .NET 5 era (2021), once planned for .NET 8, then delayed to .NET 9 and
now .NET 10. We will have to wait quite a long time as the talented Microsoft engineers are
[wasting their time on the AI/Cloud hype](https://www.youtube.com/watch?v=otokHS2PScc)...

## Goodbye to JavaScript

With the tricks above, we can basically say bye-bye to JavaScript in most cases.

We could even go further and build an entire type-safe wrapper for the JavaScript browser API based
on `IJS[InProcess]Runtime` similar to what
[`Trungnt2910.Browser`](https://github.com/trungnt2910/DotnetBrowser) did. With this much uni work
though, I cannot revive that project myself right now.

That said, for performance and maintainability reasons, if your project has substantial script
portions, it is still best to isolate these parts (and maybe use something like TypeScript instead
of pure JavaScript) from C# code. These tricks are only best when you want to avoid creating a
whole script to just click a button or get the document title.

Thanks for visiting my site, and, as usual, happy coding!
