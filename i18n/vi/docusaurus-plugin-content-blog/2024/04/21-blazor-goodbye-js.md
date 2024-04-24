---
slug: blazor-goodbye-js
title: Cách quét sạch JavaScript khỏi ứng dụng Blazor
authors: trungnt2910
tags: [C#, JS, .NET, Blazor]
---

Gần đây việc học tốn nhiều thời gian quá, nên là mình không thể dành nhiều thời gian cho Dự án
Hiện thực (_Project Reality_) và `lxmonika`. Tuy nhiên, trong lúc đang xây ứng dụng mạng bằng
Blazor để làm bài tập, mình có một số mẹo để kết hợp .NET với JS (_interop_) mình muốn chia sẻ!

<!-- truncate -->

## Điều kiện

- Hiểu biết về JavaScript và
[Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor).
- Hiểu biết về tương tác
[(_interop_)](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability)
giữa .NET/Blazor và JavaScript.

## Vấn đề

Điều mình thấy khó chịu nhất khi dùng Blazor có lẽ là những lúc cần phải động đến một chút
JavaScript. Trong khi các thành phần HTML, và trong một số trường hợp, CSS (thông qua
`Element.style`), có thể được khai báo trực tiếp trong các thành phần (_component_) của Razor, việc
chạy các mẩu JavaScript không hề đơn giản.

[Cách hay dùng nhất](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/location-of-javascript)
là tạo một tệp riêng cho những đoạn JavaScript và khai báo chúng trong trang chủ `App.razor` dưới
dạng các thẻ `<script>`. Đoạn JavaScript này cần bổ sung một vài hàm vào miền tên (_namespace_)
chung để chúng có thể được gọi bằng [`IJSRuntime.InvokeAsync`](https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/call-javascript-from-dotnet).

Việc này rắc rối nhất trong những lúc phải làm một vài điều lặt vặt Blazor không hỗ trợ, kiểu như
[bấm một nút](https://stackoverflow.com/questions/69042684/is-there-a-blazor-way-of-simulating-a-click-event-on-an-html-element)
(`button.click()`) hay nộp một mẫu `<form>` của HTML (`form.submit()`). Theo hướng trong "sách giáo
khoa", ta sẽ phải đẻ ra thêm một tệp JavaScript mới, đút thêm một thẻ `<script>`, và bôi thêm vài
hàm vào miền tên chung chỉ để thực hiện các thao tác đáng lẽ chỉ cần một dòng.

## Vài điều cần biết

### Bên trong `IJSRuntime`

Trước khi tiếp tục, hãy đào xem cái `IJSRuntime` này làm trò gì ở hậu trường.

Khi `IJSRuntime.InvokeAsync` gọi tới môi trường JavaScript của trình duyệt, nó tách tên hàm trong
tham số đầu tiên ([`identifier`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.jsinterop.ijsruntime.invokeasync?view=aspnetcore-8.0#microsoft-jsinterop-ijsruntime-invokeasync-1(system-string-system-object())))
tại vị trí của các dấu `.`. Sau đó, hàm này truy theo tên các thành viên, bắt đầu từ `window`:

(Trích từ [`Microsoft.JSInterop.ts`](https://github.com/dotnet/aspnetcore/blob/7033ec7f402fa4b7df2bf72a5e3dff1df6831f57/src/JSInterop/Microsoft.JSInterop.JS/src/src/Microsoft.JSInterop.ts#L24-L68)):
```ts
  class JSObject {
      // [đã lược một số phần]

      public findFunction(identifier: string) {
          // [đã lược một số phần]

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

          // [đã lược một số phần]
  }

  const windowJSObjectId = 0;
  const cachedJSObjectsById: { [id: number]: JSObject } = {
      [windowJSObjectId]: new JSObject(window)
  };
```

Tại sao chương trình bắt đầu tìm kiếm từ `window` nhỉ? Hãy theo mình đến phần tiếp theo.

### Tên chung (_global symbols_) và `window`

Trong môi trường chạy JavaScript ở trình duyệt, miền tên chung chính là đối tượng `window`. Tất cả
những gì có thể truy cập được toàn chương trình cũng có thể được nhắc đến bằng `window.`. Ví dụ như,

```js
var foo = 2910;
console.log(foo);
console.log(window.foo);
```

sẽ in ra:

```js
2910
2910
```

Điều này không chỉ đúng với các biến hay các tên do người dùng khai báo thêm. Các lớp (_class_) hay
hàm (_function_) cũng có thể được sử dụng như vậy:

```js
window.JSON
```

sẽ in ra (ít nhất trên Microsoft Edge):

```js
JSON {Symbol(Symbol.toStringTag): 'JSON', parse: ƒ, stringify: ƒ, rawJSON: ƒ, isRawJSON: ƒ}
```

### Điều kiện của tên hàm truyền cho `IJSRuntime.InvokeAsync`

Nắm trong tay phương pháp tìm kiếm và mã nguồn, ta có thể xác định một vài điều kiện của một biểu
thức JavaScript truyền cho hàm `IJSRuntime.InvokeAsync`:
- Biểu thức này bắt buộc phải là một **hàm (_function_)**. Trái lại, điều kiện
`result instanceof Function` sẽ bị vi phạm.
- Biểu thức này bắt buộc phải là một **chuỗi các giá trị, phân tách bởi dấu `"."`**. Những ví dụ sau
sẽ vi phạm:
    + `window["alert"]`: Tuy trả về cùng hàm với `alert`, `JSObject` sẽ không thể truy cập được do
    nó sẽ hiểu cái này thành một thành viên tên là `"window[\"alert\"]"` trong `window`.
    + `(function(a) {return window.alert(a)})`: `IJSRuntime` và những hàm đằng sau không sử dụng
    `eval`, và vì vậy không thể dịch đoạn mã này. Phần TypeScript ở trên sẽ phân tách biểu thức kia
    thành một thứ giống như `window["(function(a) {return window"]["alert(a)})"]`.
- **Không kiểu dữ liệu cơ bản (_primitive_)** được xuất hiện trong chuỗi giá trị này. Biểu thức sau
    sẽ vi phạm: `document.title.valueOf`:
    ```
    Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: Cannot use 'in' operator to search for 'valueOf' in Home
      TypeError: Cannot use 'in' operator to search for 'valueOf' in Home
    ```
    `"Home"` ở đây chính là xâu (`string`, một loại dữ liệu cơ bản của JavaScript) giá trị của
    `document.title` cho trang thử nghiệm. Điều kiện đã bị vi phạm là `if (segment in result)`, với
    `result` lúc đó là `document.title` hay (`"Home"`), còn `segment` là `"valueOf"`.

    Điều tương tự cũng xảy ra với các loại dữ liệu cơ bản khác như số (`number`) (một ví dụ là
    `document.body.childNodes.length.valueOf`) hay `boolean`.

    Về hàm `valueOf`, ta sẽ đi sâu hơn trong các mục tiếp theo.

## Giải pháp

### Hàm (_function_)

Quay lại về ví dụ [bấm nút](https://stackoverflow.com/questions/69042684/is-there-a-blazor-way-of-simulating-a-click-event-on-an-html-element).
Ta có một tham chiếu (_reference_) đến một cái nút (thường là dưới dạng `ElementReference`), giờ ta
muốn gọi hàm `click` trên tham chiếu đó.

Blazor không cho phép chúng ta trực tiếp gọi hàm JavaScript trên `ElementReference`, nhưng
`IJSRuntime` có thể dịch nó thành giá trị JavaScript tương ứng. Ta cũng được biết là `<button>`
trong HTML có một lớp JavaScript tương ứng tên là
[`HTMLButtonElement`](https://developer.mozilla.org/en-US/docs/Web/API/HTMLButtonElement).

Từ đây, ta có thể dễ dàng truy cập hàm `click` từ đối tượng mẫu (_prototype_):

```js
window.HTMLButtonElement.prototype.click
```

Là thành viên của `prototype`, hàm `click` này gắn liền với đối tượng mẫu. Để gọi với `this` được
đặt bằng tham chiếu nút bấm hiện có, chúng ta có thể dùng
[`.call`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Function/call):

Từ đây, ta có tên hàm:
```js
window.HTMLButtonElement.prototype.click.call
```

Giờ ta chỉ cần truyền tham chiếu nút bấm vào hàm trên.
Ta có thể thử chạy trên Blazor:

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

### Giá trị (_attribute_/_property_)

[`Reflect`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect),
một đối tượng trong miền tên chung của JavaScript, có những tiện ích hữu dụng trong việc xử lý các
giá trị trong JavaScript.

#### Truy cập `window`

Để truy cập bất kỳ thứ gì khác, ta cần lấy một tham chiếu đến `window` hoặc một đối tượng khác trong
miền tên chung như `document`. Với hầu hết các đối tượng JavaScript, ta có thể dùng
[`valueOf`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Object/valueOf)
để trả về gíá trị chính nó:

```cs
await using var window = await JSRuntime.InvokeAsync<IJSObjectReference>("window.valueOf");
```

Hay đơn giản hơn là:

```cs
await using var window = await JSRuntime.InvokeAsync<IJSObjectReference>("valueOf");
```

Mẹo tương tự có thể được áp dụng với các đối tượng chung khác như `document`.

#### Đọc

Để đọc giá trị, trước tiên, ta cần một tham chiếu. Sau đó, ta có thể gọi
[`Reflect.get`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/get)
trên tham chiếu đó:

```js
let ref = document.valueOf();
let title = Reflect.get(ref, "title");
console.log(title);
```

Trong Blazor thì sẽ:

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

Một giải pháp thay thế cho việc đọc giá trị là tận dụng `valueOf`, tương tự với những gì ta đã thấy
với `window` và `document`. Lợi ích của cách này là ta tiết kiệm được một tham chiếu cho đối tượng
JavaScript. Tuy nhiên, như đã nhắc đến ở trên, `valueOf` sẽ không hoạt động với các kiểu dữ liệu
cơ bản do .NET sẽ cố gắng tìm thành viên đó bằng thao tác `in`. Hơn nữa, `valueOf` có thể bị khai
báo đè (_override_) bởi một số lớp con để trả một giá trị khác với chính nó.

#### Viết

Để thay đổi giá trị, ta có thể dùng
[`Reflect.set`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/set)
trên một tham chiếu tương tự:

```js
let ref = document.valueOf();
Reflect.set(ref, "title", "Just Monika!");
```

Trong Blazor:

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

### Hàm khởi tạo (_constructor_)

Bây giờ, khi ta đã có thể điều khiển tất cả các đối tượng JavaScript mà chương trình C# chạm được
tới, nhiệm vụ cuối cùng trước khi có thể nhổ tận gốc JavaScript là tự tạo các đối tượng mới từ C#.
Nói cách khác, ta cần phải tìm cách gọi được các trình khởi tạo (_constructor_).

#### Các đối tượng JavaScript truyền thống, không phải `class`

Một số hàm khởi tạo JavaScript có thể được gọi bằng `<Lớp>.prototype.constructor` hay trực tiếp
bằng chính tên `<Lớp>`:

```js
// Sẽ đóng gói (box) giá trị dữ liệu cơ bản này
Object(2910);
// Làm thế này cũng được
Object.prototype.constructor(2910);
```

Kết quả:

```js
Number {2910}
Number {2910}
```

#### Các đối tượng JavaScript là `class`

Tuy nhiên, [một số hàm khởi tạo khác không thể được gọi kiểu này](https://stackoverflow.com/questions/30689817/es6-call-class-constructor-without-new-keyword):

```js
Set.prototype.constructor("lxmonika");
```

Kết quả:

```js
Uncaught TypeError: Constructor Set requires 'new'
    at Set (<anonymous>)
    at <anonymous>:1:15
```

Đây là những lớp được khai báo với từ khóa
[`class` trong ES6](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Classes).


#### Thử nghiệm giải pháp tiềm năng - `Reflect.construct`

Để gỡ bỏ rào cản của từ khóa `new`, ta có thể sử dụng hàm
[`Reflect.construct`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Reflect/construct):

Với những lớp này, ta kiếm một tham chiếu đến hàm khởi tạo. Sau đó, ta sẽ đút toàn bộ tham số khởi
tạo vào một mảng JavaScript, rồi gọi `Reflect.construct`:

```js
let ctor = Set.bind();
let args = Array("lxmonika")
Reflect.construct(ctor, args);
```

Trong Blazor, có thể viết kiểu như:

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

Trông ổn phết nhỉ? Nhưng không:

```
Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: Cannot create a JSObjectReference from the value 'function () { [native code] }'.
      Error: Cannot create a JSObjectReference from the value 'function () { [native code] }'.
```

Microsoft có đặt ra một
[giới hạn vô lý](https://github.com/dotnet/aspnetcore/blob/7033ec7f402fa4b7df2bf72a5e3dff1df6831f57/src/JSInterop/Microsoft.JSInterop.JS/src/src/Microsoft.JSInterop.ts#L148)
không cho các `function` của JavaScript được gắn vào `JSObjectReference`:

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

Mình đã nêu vấn đề này với Microsoft ở
[đây](https://github.com/dotnet/aspnetcore/discussions/55259), mong rằng một ngày họ đưa ra hướng
giải quyết hay.

#### `eval`

Cho đến giờ, chúng ta đã cố gắng hết sức để né `eval`, không chỉ vì hàm đó dính nhiều tai tiếng liên
quan đến các lỗ hổng bảo mật, mà còn do nhiều trang mạng hiện đại áp dụng một số luật
(_[Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)_) ngăn chạn không
cho mã JavaScript truy cập hàm đó. `eval` thực sự mạnh đến mức riêng mình nó có thể giải quyết mọi
vấn đề gọi hàm/đọc giá trị/viết giá trị ở trên, và thêm cả việc gọi hàm khởi tạo cho chúng ta:

```cs
await using var set = await JSRuntime.InvokeAsync<IJSObjectReference>(
    "window.eval", @"new Set(""lxmonika"")"
);
```

#### Hàm hỗ trợ

Nếu bằng mọi giá không thể dùng `eval`, đường duy nhất còn lại là sử dụng một hàm hỗ trợ:

```js
function constructByName(name, ...args) {
    return Reflect.construct(window[name], args);
}
constructByName("Set", "lxmonika");
```

Nhưng thật ý, làm gì mà đang dùng Blazor mà lại cần một cái `Set` của JavaScript đến mức vậy?

## Future .NET

ASP.NET Core có [dự định](https://github.com/dotnet/aspnetcore/issues/31151) hỗ trợ truy cập trình
khởi tạo, gọi các hàm, và đọc/viết giá trị từ chương trình Blazor.

Đề xuất này đã được mở từ thời .NET 5 (2021), từng được dự kiến triển khai trong .NET 8, rồi rời đến
.NET 9 và giờ là .NET 10. Chúng ta có lẽ sẽ phải chờ khá lâu trong lúc Microsoft đang lãng phí nhân
tài [chạy đua vũ trang giữa cơn sốt AI/Cloud](https://www.youtube.com/watch?v=otokHS2PScc)...

## Quét sạch JavaScript

Với các mẹo ở trên, chúng ta gần như có thể loại trừ JavaScript trong hầu hết trường hợp.

Tham vọng hơn, ta có thể tạo một thư viện bọc giao diện JavaScript trình duyệt với đầy đủ thông tin
kiểu dữ liệu giống như [`Trungnt2910.Browser`](https://github.com/trungnt2910/DotnetBrowser) nhưng
dựa trên `IJS[InProcess]Runtime`. Tuy nhiên, trong giai đoạn này, ngập trong một núi bài ở trường,
bản thân mình sẽ chưa thể vực lại dự án đó dậy.

Nói vậy nhưng, vì lý do hiệu suất và duy trì lâu dài, nếu dự án chứa rất nhiều JavaScript, tốt nhất
vẫn là tách những phần đó ra (và sử dụng một ngôn ngữ như TypeScript cho tiện hơn so với JavaScript
thuần) khỏi mã nguồn C#. Những mẹo này chỉ tối ưu trong trường hợp cần tránh tạo cả một tệp mới chỉ
để bấm một nút hay lấy tiêu đề của trang.

Cảm ơn mọi người đã ghé thăm trang của mình, và chúc mọi người một ngày lập trình vui vẻ!
