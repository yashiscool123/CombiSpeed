/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{js,ts,jsx,tsx}",
  ],
  theme: {
    extend: {
      colors: {
        cyber: {
          bg: "#080c14",
          surface: "#0f172a",
          card: "#131d33",
          border: "#1e293b",
          primary: "#00f0ff",
          emerald: "#10b981",
          violet: "#8b5cf6",
          rose: "#f43f5e",
          amber: "#f59e0b",
        }
      },
      fontFamily: {
        mono: ["Consolas", "Monaco", "Courier New", "monospace"],
        sans: ["Inter", "system-ui", "-apple-system", "sans-serif"]
      },
      animation: {
        'pulse-slow': 'pulse 3s cubic-bezier(0.4, 0, 0.6, 1) infinite',
        'glow': 'glow 2s ease-in-out infinite alternate',
      },
      keyframes: {
        glow: {
          '0%': { boxShadow: '0 0 10px rgba(0, 240, 255, 0.3)' },
          '100%': { boxShadow: '0 0 25px rgba(0, 240, 255, 0.7)' },
        }
      }
    },
  },
  plugins: [],
}
