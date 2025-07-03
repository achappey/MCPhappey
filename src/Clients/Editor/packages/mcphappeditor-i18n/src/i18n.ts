import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import en from './locales/en.json';
import nl from './locales/nl.json';
import LanguageDetector from 'i18next-browser-languagedetector';

export const initI18n = () => {
  if (i18n.isInitialized) return i18n;
  i18n
    .use(LanguageDetector) // plugin toevoegen!
    .use(initReactI18next)
    .init({
      resources: { en: { translation: en }, nl: { translation: nl } },
      //lng,
      fallbackLng: 'en',
      interpolation: { escapeValue: false },
      supportedLngs: ['en', 'nl'],
      detection: {
        order: ['querystring', 'cookie', 'localStorage', 'navigator', 'htmlTag'],
        lookupQuerystring: 'lng',
        lookupCookie: 'i18next',
        lookupLocalStorage: 'i18nextLng',
        caches: ['localStorage', 'cookie']
      },
    });
  return i18n;
}
