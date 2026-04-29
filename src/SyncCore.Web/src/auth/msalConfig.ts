import { Configuration, LogLevel } from '@azure/msal-browser';

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_B2C_CLIENT_ID as string,
    authority: `${import.meta.env.VITE_B2C_INSTANCE}/${import.meta.env.VITE_B2C_DOMAIN}/${import.meta.env.VITE_B2C_POLICY}`,
    knownAuthorities: [import.meta.env.VITE_B2C_DOMAIN as string],
    redirectUri: window.location.origin,
    postLogoutRedirectUri: window.location.origin,
  },
  cache: {
    cacheLocation: 'sessionStorage',
    storeAuthStateInCookie: false,
  },
  system: {
    loggerOptions: {
      loggerCallback: (level, message, containsPii) => {
        if (containsPii) return;
        if (level === LogLevel.Error) console.error(message);
      },
    },
  },
};

export const loginRequest = {
  scopes: [import.meta.env.VITE_B2C_SCOPE as string],
};
