import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

const config: Config = {
  title: 'My Reality',
  tagline: 'Trung Nguyen\'s blog on software development and other random stuff',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://trungnt2910.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'trungnt2910', // Usually your GitHub org/user name.
  projectName: 'trungnt2910.github.io', // Usually your repo name.

  onBrokenLinks: 'throw',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'en',
    locales: ['vi', 'en', 'ja'],
    localeConfigs: {
      en: {
        htmlLang: 'en-US'
      }
    }
  },

  presets: [
    [
      'classic',
      {
        docs: false,
        blog: {
          routeBasePath: '/',
          showReadingTime: true
        },
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      title: 'My Reality',
      logo: {
        alt: 'My Reality',
        src: 'img/logo.svg',
      },
      items: [
        {
          href: 'https://github.com/trungnt2910',
          label: 'GitHub',
          position: 'right',
        },
        {
          type: 'localeDropdown',
          position: 'right',
        }
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: 'Projects',
          items: [
            {
              label: 'lxmonika',
              href: 'https://github.com/trungnt2910/lxmonika',
            },
            {
              label: 'MemoryModule.NET',
              href: 'https://github.com/trungnt2910/MemoryModule.NET',
            },
            {
              label: 'HyClone',
              href: 'https://github.com/trungnt2910/hyclone',
            },
          ],
        },
        {
          title: 'Connect with me',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/trungnt2910',
            },
            {
              label: 'LinkedIn',
              href: 'https://linkedin.com/in/trungnt2910',
            },
            {
              label: 'Discord (Project Reality)',
              href: 'https://discord.gg/c9xaScvazh',
            },
          ],
        }
      ],
      copyright: `Copyright © ${(new Date().getFullYear() === 2024 ? "2024" : `2024 - ${new Date().getFullYear()}`)} Trung Nguyen. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
