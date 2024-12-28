import { defineConfig } from 'vite';
import biomePlugin from 'vite-plugin-biome';
import checker from 'vite-plugin-checker';

export default defineConfig(({ mode }) => ({
  appType: 'custom',
  root: 'Assets',
  build: {
    emptyOutDir: true,
    manifest: true,
    outDir: '../wwwroot',
    assetsDir: '',
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
    watch: {
      usePolling: true,
    },
  },
  plugins: [
    biomePlugin({
      mode: 'check',
      applyFixes: mode === 'serve',
    }),
    checker({
      typescript: true,
    }),
  ],
}));
