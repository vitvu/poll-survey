/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './public/index.html',
    './src/**/*.{vue,js,ts,jsx,tsx}',
  ],
  theme: {
    extend: {
      colors: {
        blue: {
          DEFAULT: '#2563eb',
          hover: '#1d4ed8',
          light: '#eff6ff',
          border: '#bfdbfe',
          dark: '#1e40af',
        },
        green: {
          DEFAULT: '#16a34a',
          light: '#dcfce7',
        },
        red: {
          DEFAULT: '#dc2626',
          light: '#fee2e2',
        },
        amber: { DEFAULT: '#d97706' },
      },
      fontFamily: {
        sans: ['Inter', '-apple-system', 'sans-serif'],
      },
      borderRadius: {
        DEFAULT: '8px',
        md: '10px',
        lg: '12px',
        full: '9999px',
      },
      boxShadow: {
        card: '0 1px 3px rgba(0,0,0,.08), 0 1px 2px rgba(0,0,0,.04)',
      },
    },
  },
  plugins: [],
}
