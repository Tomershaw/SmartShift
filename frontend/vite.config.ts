import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  css: {
    postcss: "./postcss.config.js", // טוען את הקובץ PostCSS בו מוגדר Tailwind
  },
  build: {
    outDir: "../SmartShift.Api/wwwroot",
    emptyOutDir: true,
  },
  server: {
    proxy: {
      "/api": {
        target: "https://localhost:7001",
        secure: false,
        changeOrigin: true,
      },
    },
  },
});
