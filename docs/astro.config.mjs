// @ts-check
import { defineConfig } from 'astro/config';
import starlight from '@astrojs/starlight';
import corsinvestTheme from '@corsinvest/cv4pve-docs-theme';

export default defineConfig({
  site: 'https://corsinvest.github.io',
  base: '/cv4pve-cli',
  integrations: [
    starlight({
      title: 'cv4pve-cli',
      description: 'The whole Proxmox VE API from the command line, like kubectl for Kubernetes: saved contexts for several clusters, 327 built-in aliases and tab completion that reads the live cluster.',
      // Brand, logo, GitHub and "Edit page" links, the Corsinvest sidebar group and
      // external links in a new tab come from the shared cv4pve theme.
      plugins: [
        corsinvestTheme({
          repo: 'cv4pve-cli',
          // Product icon: favicon and header, dark variant for the dark theme.
          icon: { light: '/icon.svg', dark: '/icon-dark.svg' },
          // Visits, without cookies.
          matomo: { url: 'https://matomo.corsinvest.it/', siteId: 10 },
          // Install-and-run panel in the home hero.
          install: {
            targets: ['linux', 'macos', 'windows'],
            run: ['api get /nodes'],
          },
        }),
      ],
      lastUpdated: true,
      sidebar: [
        {
          label: 'Start here',
          items: ['getting-started', 'contexts', 'permissions', 'coming-from-pvesh', 'troubleshooting'],
        },
        {
          label: 'Using cv4pve-cli',
          items: ['api', 'aliases', 'tasks', 'completion', 'scripting', 'ai-agents'],
        },
        {
          label: 'Reference',
          items: ['reference/commands', 'reference/aliases', 'reference/files'],
        },
      ],
    }),
  ],
});
