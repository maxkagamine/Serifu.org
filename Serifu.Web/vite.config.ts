import type { UserConfig } from 'vite';

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
} satisfies UserConfig;
