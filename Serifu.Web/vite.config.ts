import { defineConfig } from 'vite';
import biomePlugin from 'vite-plugin-biome';
import checker from 'vite-plugin-checker';

export default defineConfig(({ command }) => ({
  appType: 'custom',
  root: 'Assets',
  build: {
    emptyOutDir: true,
    manifest: true,
    outDir: '../wwwroot',
    assetsDir: 'assets',
    rollupOptions: {
      input: 'Assets/main.ts',
    },
    cssMinify: 'lightningcss',
  },
  css: {
    transformer: 'lightningcss',
  },
  server: {
    strictPort: true,
    host: '127.0.0.1',
  },
  plugins: [
    biomePlugin({
      mode: 'check',
      applyFixes: command === 'serve',
    }),
    checker({
      typescript: true,
    }),
  ],
}));
