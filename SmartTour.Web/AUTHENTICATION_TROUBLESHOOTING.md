# Troubleshooting & Fix: Blazor Server API Authentication

## Problem Description
When making API calls (e.g., Saving a POI) from the Blazor Web App to the API, the request fails with `401 Unauthorized`.
Logs indicate that the `X-SmartTour-User-Id` header is missing or null, even though the user appears logged in on the UI.

## Root Causes

### 1. Missing Identity Claims (AccountController)
*   **Issue:** The `AccountController` was creating a customized `ClaimsPrincipal` but only added a custom `"UserId"` claim types. It **missed** the standard `ClaimTypes.NameIdentifier`.
*   **Impact:** `MainLayout` (and Blazor's `AuthenticationStateProvider`) looks for `ClaimTypes.NameIdentifier` by default. When it couldn't find it, it returned `null` for the User ID, so the Session Service was never populated.
*   **Fix:** Updated `AccountController.cs` to explicitly add `new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())` during Login (Google & Local).

### 2. Dependency Injection Scope Mismatch (The "Ghost" Service)
*   **Issue:** Blazor Server uses a **Circuit Scope** (one scope per user connection). However, `IHttpClientFactory` (used by `AddHttpClient`) creates a **separate internal scope** for constructing HTTP Message Handlers.
*   **Impact:** 
    *   `MainLayout` injected `UserSessionService` from the **Circuit Scope** (Instance A) and successfully set the User ID.
    *   `ServerSideAuthHandler` (inside HttpClient) injected `UserSessionService` from the **Factory Handler Scope** (Instance B).
    *   **Result:** Instance B was empty (UserId = null), while Instance A had the data. The Handler checked the empty instance and failed to add the Auth Header.
*   **Fix:** 
    *   Removed `builder.Services.AddHttpClient(...).AddHttpMessageHandler<ServerSideAuthHandler>()`.
    *   Manually registered the `HttpClient` as a **Scoped Service** in `Program.cs`:
        ```csharp
        builder.Services.AddScoped(sp => 
        {
            // Resolve Handler from the CURRENT Circuit Scope (sp)
            var authHandler = sp.GetRequiredService<ServerSideAuthHandler>();
            authHandler.InnerHandler = new HttpClientHandler();
            
            // Create Client manually
            return new HttpClient(authHandler) { BaseAddress = ... };
        });
        ```
    *   This forces the Handler to live in the same Scope as the Blazor Circuit, sharing the correct `UserSessionService` instance.

## Architecture Overview (BFF Pattern)
1.  **Login:** User logs in via Google/Local -> Cookie is set with `NameIdentifier`.
2.  **Sync:** `MainLayout.OnParametersSetAsync` reads the `AuthenticationState` (from Cookie) and pushes the UserID into `UserSessionService`.
3.  **API Call:** Component injects `HttpClient`. `HttpClient` uses `ServerSideAuthHandler`.
4.  **Auth Header:** `ServerSideAuthHandler` reads UserID from `UserSessionService` (now valid) and adds `X-SmartTour-User-Id` header + API Key.
5.  **API Middleware:** API's `ApiKeyMiddleware` validates Key + UserID and establishes Identity.

## Debugging Tips
*   Check "Instance ID" logs of `UserSessionService` to ensure `MainLayout` and `Handler` are using the same object guid.
*   Ensure `appsettings.json` has the matching `SmartTourApiKey` in both projects.
