import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ThemeToggle } from './ThemeToggle'
import { STORAGE_KEY } from './theme'

describe('ThemeToggle', () => {
  it('ist als Schalter beschriftet und meldet seinen Zustand', async () => {
    render(<ThemeToggle variant="switch" />)
    const toggle = screen.getByRole('switch', { name: 'Dunkles Design' })

    expect(toggle).toHaveAttribute('aria-checked', 'false')

    await userEvent.click(toggle)

    expect(toggle).toHaveAttribute('aria-checked', 'true')
    expect(document.documentElement).toHaveClass('dark')
    expect(localStorage.getItem(STORAGE_KEY)).toBe('dark')
  })

  it('nennt als Symbolknopf die Handlung, nicht den Zustand', async () => {
    render(<ThemeToggle variant="icon" />)

    await userEvent.click(screen.getByRole('button', { name: 'Dunkles Design einschalten' }))

    expect(screen.getByRole('button', { name: 'Helles Design einschalten' })).toBeInTheDocument()
  })

  // Kopfzeile und aufgeklappte Seitenleiste stehen gleichzeitig im DOM.
  it('hält zwei Umschalter gleich', async () => {
    render(
      <>
        <ThemeToggle variant="switch" />
        <ThemeToggle variant="icon" />
      </>,
    )

    await userEvent.click(screen.getByRole('button', { name: 'Dunkles Design einschalten' }))

    expect(screen.getByRole('switch', { name: 'Dunkles Design' })).toHaveAttribute('aria-checked', 'true')
  })
})
