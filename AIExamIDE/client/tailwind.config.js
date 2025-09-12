/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./**/*.{razor,html,cshtml}",
    "./**/*.cs",
    "./Components/**/*.{razor,cs}",
    "./Pages/**/*.{razor,cs}"
  ],
  theme: {
    extend: {
      fontFamily: {
        'mono': ['Consolas', 'monospace']
      }
    },
  },
  plugins: [],
}
