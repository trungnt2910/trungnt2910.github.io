import clsx from 'clsx';
import * as React from "react";
import Heading from '@theme/Heading';
import Translate from '@docusaurus/Translate';

export default function BlogHeader() {
  return (
    <header className={clsx('hero hero--primary')} style={{
        paddingTop: "32px",
        paddingBottom: "32px",
        marginBottom: "32px",
        width: "100%"
    }}>
      <div className="container">
        <Heading as="h1" className="hero__title" style={{ textAlign: "center" }}>
            <Translate id="header.title">My Reality</Translate>
        </Heading>
        <p className="hero__subtitle" style={{ textAlign: "center" }}>
            <Translate id="header.subtitle">
                Trung Nguyen's blog on software development and other random stuff.
            </Translate>
        </p>
      </div>
    </header>
  );
}
