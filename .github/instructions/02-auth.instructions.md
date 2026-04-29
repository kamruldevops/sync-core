---
applyTo: "**"
---

# Feature 02 — Authentication (Azure AD B2C)

## Goal
Protect every API endpoint with Azure AD B2C JWT tokens. The React app silently acquires tokens and attaches them to every API request. Users who are not signed in are redirected to the B2C sign-in page.

---

## Prerequisites
- Azure AD B2C tenant created
- App registration created for the API (expose a scope: `api://<clientId>/Photos.ReadWrite`)
- App registration created for the SPA (redirect URI: `http://localhost:5173`)
- A **Sign up and sign in** user flow created (e.g. `B2C_1_signupsignin`)

---

## Backend — `SyncCore.Api`

### NuGet
```
Microsoft.Identity.Web   (already added in setup)
```

### `Program.cs` additions
```csharp
builder.Services
    .AddMicrosoftIdentityWebApiAuthentication(builder.Configuration, "AzureAdB2C");

// ...after build...
app.UseAuthentication();
app.UseAuthorization();
```

### `appsettings.json`
```json
"AzureAdB2C": {
  "Instance": "https://<tenant>.b2clogin.com",
  "ClientId": "<api-app-client-id>",
  "Domain": "<tenant>.onmicrosoft.com",
  "SignUpSignInPolicyId": "B2C_1_signupsignin"
}
```

### Helper — resolve current user ID
Create `Infrastructure/Auth/CurrentUser.cs`:
```csharp
public static class CurrentUser
{
    // Returns the B2C object ID (oid claim) as a string
    public static string GetUserId(ClaimsPrincipal principal)
        => principal.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? throw new UnauthorizedAccessException("No user identity found.");
}
```

Inject `IHttpContextAccessor` wherever the current user ID is needed in handlers.

### Protect all endpoints
Add a fallback authorization policy so every endpoint requires auth by default:
```csharp
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());
```

---

## Frontend — `SyncCore.Web`

### MSAL config — `src/auth/msalConfig.ts`
```ts
import { Configuration, LogLevel } from "@azure/msal-browser";

export const msalConfig: Configuration = {
  auth: {
    clientId: import.meta.env.VITE_B2C_CLIENT_ID,
    authority: import.meta.env.VITE_B2C_AUTHORITY,          // https://<tenant>.b2clogin.com/<tenant>.onmicrosoft.com/B2C_1_signupsignin
    knownAuthorities: [import.meta.env.VITE_B2C_KNOWN_AUTHORITY], // <tenant>.b2clogin.com
    redirectUri: import.meta.env.VITE_B2C_REDIRECT_URI,
  },
  cache: { cacheLocation: "sessionStorage" },
};

export const loginRequest = {
  scopes: [import.meta.env.VITE_B2C_API_SCOPE],            // api://<clientId>/Photos.ReadWrite
};
```

### Axios interceptor — `src/api/client.ts`
```ts
import { msalInstance } from "../auth/msalInstance";
import { loginRequest } from "../auth/msalConfig";

apiClient.interceptors.request.use(async (config) => {
  const account = msalInstance.getAllAccounts()[0];
  if (account) {
    const result = await msalInstance.acquireTokenSilent({ ...loginRequest, account });
    config.headers.Authorization = `Bearer ${result.accessToken}`;
  }
  return config;
});
```

### Auth guard component — `src/auth/RequireAuth.tsx`
```tsx
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { loginRequest } from "./msalConfig";

export function RequireAuth({ children }: { children: React.ReactNode }) {
  const { instance } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  if (!isAuthenticated) {
    instance.loginRedirect(loginRequest);
    return null;
  }
  return <>{children}</>;
}
```

### Wrap routes
In `App.tsx`, wrap all authenticated routes with `<RequireAuth>`.

### Sign-out button
```tsx
const { instance } = useMsal();
<button onClick={() => instance.logoutRedirect()}>Sign out</button>
```

### `.env.local` additions
```
VITE_B2C_CLIENT_ID=<spa-client-id>
VITE_B2C_AUTHORITY=https://<tenant>.b2clogin.com/<tenant>.onmicrosoft.com/B2C_1_signupsignin
VITE_B2C_KNOWN_AUTHORITY=<tenant>.b2clogin.com
VITE_B2C_REDIRECT_URI=http://localhost:5173
VITE_B2C_API_SCOPE=api://<api-client-id>/Photos.ReadWrite
```

---

## Acceptance Criteria
- [ ] Unauthenticated `GET /photos` returns HTTP 401
- [ ] Signing in via the B2C flow redirects back to the React app
- [ ] Authenticated requests include a valid Bearer token and return HTTP 200
- [ ] Signing out clears the MSAL session and redirects to the sign-in page
- [ ] Token is refreshed silently without interrupting the user
