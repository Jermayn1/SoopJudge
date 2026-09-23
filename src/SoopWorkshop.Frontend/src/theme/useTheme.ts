import { useSyncExternalStore } from 'react'
import { getTheme, setTheme, subscribe, type Theme } from './theme'

export function useTheme(): { theme: Theme; toggleTheme: () => void } {
  const theme = useSyncExternalStore(subscribe, getTheme)

  const toggleTheme = () => setTheme(theme === 'dark' ? 'light' : 'dark')

  return { theme, toggleTheme }
}
