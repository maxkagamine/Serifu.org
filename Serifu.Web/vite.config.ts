import type { UserConfig } from 'vite';
import biomePlugin from 'vite-plugin-biome';

export default {
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
      applyFixes: true,
    }),
  ],
} satisfies UserConfig;
