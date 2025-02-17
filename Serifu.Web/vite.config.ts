import type { Selector, SelectorComponent } from 'lightningcss';
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
    lightningcss: {
      visitor: {
        Rule: {
          style(rule) {
            // Since the popover polyfill (https://github.com/oddbird/popover-polyfill/) can't polyfill the
            // :popover-open pseudo-class, it adds a class name ":popover-open" to popover elements instead. This
            // transform duplicates any rules containing the pseudo-class and replaces it with the class name (combining
            // them in one selector list would not work, as it's an invalid selector to those older browsers).
            let changed = false;

            function recurseSelectors(selectors: Selector[]): Selector[] {
              return selectors.map((s: Selector) =>
                s.map((c: SelectorComponent) => {
                  if (c.type === 'pseudo-class') {
                    switch (c.kind) {
                      case 'popover-open':
                        changed = true;
                        return {
                          type: 'class',
                          name: ':popover-open',
                        };
                      case 'not':
                      case 'where':
                      case 'is':
                      case 'any':
                      case 'has':
                        return {
                          ...c,
                          selectors: recurseSelectors(c.selectors),
                        };
                      case 'nth-child':
                      case 'nth-last-child':
                        if (c.of) {
                          return {
                            ...c,
                            of: recurseSelectors(c.of),
                          };
                        }
                    }
                  }
                  return c;
                }),
              );
            }

            const selectors = recurseSelectors(rule.value.selectors);

            if (changed) {
              return [
                rule,
                {
                  ...rule,
                  value: {
                    ...rule.value,
                    selectors,
                  },
                },
              ];
            }

            return rule;
          },
        },
      },
    },
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
