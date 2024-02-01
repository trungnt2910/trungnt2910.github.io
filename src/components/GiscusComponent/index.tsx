import React from 'react';
import Giscus from "@giscus/react";
import { useColorMode } from '@docusaurus/theme-common';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import BrowserOnly from '@docusaurus/BrowserOnly';

export default function GiscusComponent() {
  const { colorMode } = useColorMode();
  const { i18n } = useDocusaurusContext();

  return (
    <BrowserOnly>
        {() =>
            <Giscus
                repo="trungnt2910/trungnt2910.github.io"
                repoId="R_kgDOLMhw3Q"
                category="Blogs"
                categoryId="DIC_kwDOLMhw3c4Cc3fr"
                mapping="specific"
                term={location.pathname.split("/").at(-1)}
                strict="0"
                reactionsEnabled="1"
                emitMetadata="1"
                inputPosition="top"
                theme={colorMode}
                lang={i18n.currentLocale}
                loading="lazy"
            />
        }
    </BrowserOnly>
  );
}
