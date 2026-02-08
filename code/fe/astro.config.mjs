// @ts-check
import { defineConfig } from 'astro/config';

import react from '@astrojs/react';
import tailwindcss from '@tailwindcss/vite';
import cloudflare from '@astrojs/cloudflare';
import sitemap from '@astrojs/sitemap';

// https://astro.build/config
export default defineConfig({
  site: 'https://witnes.io',
  output: 'server',
  adapter: cloudflare(),
  integrations: [
    react(),
    sitemap({
      filter: (page) =>
        !page.includes('/dashboard') &&
        !page.includes('/authenticate'),
    }),
  ],

  vite: {
    plugins: [tailwindcss()],
    build: {
      rollupOptions: {
        output: {
        }
      },
      chunkSizeWarningLimit: 1000,
    },
    optimizeDeps: {
      include: [
        'react',
        'react-dom',
        'lucide-react',
        '@tanstack/react-table',
        'react-router-dom',
      ],
    },
    resolve: {
      dedupe: ['react', 'react-dom', 'react-router-dom'],
    },
  }
});