import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { pt_BR } from '../locales/pt_BR';
import type { TranslationKey } from '../locales/pt_BR';
import { en_US } from '../locales/en_US';

export type SupportedLanguage = 'pt_BR' | 'en_US';

interface LanguageContextType {
  language: SupportedLanguage;
  setLanguage: (lang: SupportedLanguage) => void;
  t: (key: TranslationKey, params?: Record<string, string | number>) => string;
  formatDate: (date: Date | string, options?: Intl.DateTimeFormatOptions) => string;
  formatTime: (date: Date | string) => string;
}

const dictionaries: Record<SupportedLanguage, Record<TranslationKey, string>> = {
  pt_BR,
  en_US
};

const LanguageContext = createContext<LanguageContextType | null>(null);

export const LanguageProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [language, setLanguageState] = useState<SupportedLanguage>(() => {
    const saved = localStorage.getItem('solar_locale') as SupportedLanguage | null;
    if (saved && (saved === 'pt_BR' || saved === 'en_US')) {
      return saved;
    }
    // Detecta idioma do navegador
    if (typeof navigator !== 'undefined' && navigator.language) {
      if (navigator.language.startsWith('en')) return 'en_US';
    }
    return 'pt_BR';
  });

  const setLanguage = useCallback((lang: SupportedLanguage) => {
    setLanguageState(lang);
    localStorage.setItem('solar_locale', lang);
    document.documentElement.lang = lang === 'pt_BR' ? 'pt-BR' : 'en-US';
  }, []);

  useEffect(() => {
    document.documentElement.lang = language === 'pt_BR' ? 'pt-BR' : 'en-US';
  }, [language]);

  const t = useCallback((key: TranslationKey, params?: Record<string, string | number>): string => {
    const dict = dictionaries[language] || dictionaries.pt_BR;
    let text = dict[key] || dictionaries.pt_BR[key] || String(key);

    if (params) {
      Object.entries(params).forEach(([paramKey, paramValue]) => {
        text = text.replace(new RegExp(`\\{${paramKey}\\}`, 'g'), String(paramValue));
      });
    }

    return text;
  }, [language]);

  const formatDate = useCallback((dateInput: Date | string, options?: Intl.DateTimeFormatOptions): string => {
    try {
      const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
      const localeCode = language === 'pt_BR' ? 'pt-BR' : 'en-US';
      return new Intl.DateTimeFormat(localeCode, options || {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
      }).format(date);
    } catch {
      return String(dateInput);
    }
  }, [language]);

  const formatTime = useCallback((dateInput: Date | string): string => {
    try {
      const date = typeof dateInput === 'string' ? new Date(dateInput) : dateInput;
      const localeCode = language === 'pt_BR' ? 'pt-BR' : 'en-US';
      return new Intl.DateTimeFormat(localeCode, {
        hour: '2-digit',
        minute: '2-digit',
        second: '2-digit',
        hour12: language === 'en_US'
      }).format(date);
    } catch {
      return String(dateInput);
    }
  }, [language]);

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t, formatDate, formatTime }}>
      {children}
    </LanguageContext.Provider>
  );
};

export const useTranslation = () => {
  const context = useContext(LanguageContext);
  if (!context) {
    throw new Error('useTranslation must be used within a LanguageProvider');
  }
  return context;
};
