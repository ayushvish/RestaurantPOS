import { useEffect } from 'react';

interface ShortcutActions {
  onSearchFocus: () => void;
  onPaymentTrigger: () => void;
  onFireKot: () => void;
  onPrintProforma: () => void;
  onEscape: () => void;
}

export const useKeyboardShortcuts = ({
  onSearchFocus,
  onPaymentTrigger,
  onFireKot,
  onPrintProforma,
  onEscape,
}: ShortcutActions) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      // Don't intercept if user is typing in regular text fields unless it's a function key or Escape
      if (e.key === 'F2') {
        e.preventDefault();
        onSearchFocus();
      } else if (e.key === 'F4') {
        e.preventDefault();
        onPaymentTrigger();
      } else if (e.key === 'F8') {
        e.preventDefault();
        onFireKot();
      } else if (e.key === 'F9') {
        e.preventDefault();
        onPrintProforma();
      } else if (e.key === 'Escape') {
        e.preventDefault();
        onEscape();
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [onSearchFocus, onPaymentTrigger, onFireKot, onPrintProforma, onEscape]);
};
