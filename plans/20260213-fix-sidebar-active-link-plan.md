# Fix Sidebar Active Link Highlighting

## Overview

When navigating to any page other than Dashboard, the Dashboard sidebar link remains highlighted (or no link is highlighted at all depending on URL shape). The active page's sidebar link should be the one highlighted.

## Root Cause Analysis

The active-link logic lives in `src/OSRSTools.Web/wwwroot/js/sidebar.js` (lines 22-40). It uses a client-side JavaScript approach that compares `window.location.pathname` against each link's `href` attribute using `startsWith`.

**The bug has two parts:**

### Problem 1: `startsWith("/")` matches everything

The Dashboard link resolves to `href="/"` (Home controller, Index action). The check on line 27 is:

```js
if (href && currentPath.startsWith(href.toLowerCase()))
```

Every pathname starts with `/`, so the Dashboard link matches **every single page**. When a user is on `/HighAlching`, `"/highalching".startsWith("/")` is `true`, so Dashboard gets the `active` class in addition to (or instead of) the correct link.

### Problem 2: Redundant root-path fallback

Lines 32-40 add a second fallback that also tries to mark Dashboard as active when on `/`. This compounds the issue and is fragile because it relies on querying specific `href` attribute values that may change.

### Why client-side matching is fragile

ASP.NET Tag Helpers generate `href` values at render time, but the exact format can vary (`/`, `/Home`, `/Home/Index`). Matching by URL string is brittle. The standard ASP.NET MVC approach is to use `ViewContext.RouteData.Values["controller"]` on the server side, which is always reliable.

## Proposed Fix

Replace the client-side JavaScript active-link logic with a server-side approach using ASP.NET MVC's `ViewContext.RouteData` in the Razor layout. This is the idiomatic pattern for MVC sidebar navigation.

## Files to Modify

| # | File | Change |
|---|------|--------|
| 1 | `src/OSRSTools.Web/Views/Shared/_Layout.cshtml` | Add server-side active class to each nav link |
| 2 | `src/OSRSTools.Web/wwwroot/js/sidebar.js` | Remove the client-side active-link highlighting logic (lines 21-40) |

## Step-by-Step Implementation

### Step 1: Update `_Layout.cshtml` -- Add server-side active class

At the top of the file (after the `<html>` tag or inside the `<body>`), add a Razor code block that reads the current controller name:

```csharp
@{
    var currentController = ViewContext.RouteData.Values["controller"]?.ToString() ?? "";
}
```

Then, for each sidebar nav link, conditionally apply the `active` CSS class based on whether the link's controller matches the current controller. Replace each `<a>` tag as follows:

**Dashboard (Home controller):**
```html
<a asp-controller="Home" asp-action="Index"
   class="nav-link @(currentController == "Home" ? "active" : "")"
   title="Dashboard">
```

**High Alching:**
```html
<a asp-controller="HighAlching" asp-action="Index"
   class="nav-link @(currentController == "HighAlching" ? "active" : "")"
   title="High Alching">
```

**Flipping:**
```html
<a asp-controller="Flipping" asp-action="Index"
   class="nav-link @(currentController == "Flipping" ? "active" : "")"
   title="Flipping">
```

**Smithing:**
```html
<a asp-controller="Smithing" asp-action="Index"
   class="nav-link @(currentController == "Smithing" ? "active" : "")"
   title="Smithing">
```

**Herblore:**
```html
<a asp-controller="Herblore" asp-action="Index"
   class="nav-link @(currentController == "Herblore" ? "active" : "")"
   title="Herblore">
```

**Export (Report controller):**
```html
<a asp-controller="Report" asp-action="Index"
   class="nav-link @(currentController == "Report" ? "active" : "")"
   title="Export">
```

### Step 2: Remove client-side active-link logic from `sidebar.js`

Remove lines 21-40 from `sidebar.js`. The entire block to remove is:

```js
    // Highlight active nav link based on current URL
    const currentPath = window.location.pathname.toLowerCase();
    const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');

    navLinks.forEach(function (link) {
        const href = link.getAttribute('href');
        if (href && currentPath.startsWith(href.toLowerCase())) {
            link.classList.add('active');
        }
    });

    // If on root path, highlight Dashboard
    if (currentPath === '/' || currentPath === '') {
        const dashboardLink = document.querySelector('a[href="/"]') ||
            document.querySelector('a[href="/Home"]') ||
            document.querySelector('a[href="/Home/Index"]');
        if (dashboardLink) {
            dashboardLink.classList.add('active');
        }
    }
```

After removal, `sidebar.js` should only contain the sidebar toggle/collapse functionality (lines 1-19 and the closing `});`).

## No CSS Changes Required

The existing CSS rule at line 199-203 of `site.css` already correctly styles `.nav-link.active`:

```css
.sidebar-nav .nav-link.active {
    background-color: var(--sidebar-active);
    color: var(--accent);
    border-right: 3px solid var(--accent);
}
```

This will continue to work as-is since we are still applying the `active` class -- just from the server side now.

## Token Estimate

- **Low:** 2,000 tokens
- **High:** 4,000 tokens

This is a small, focused change touching two files with a clear pattern to follow.

## Verification Steps

1. **Build succeeds:** `dotnet build` completes with 0 errors
2. **All existing tests pass:** `dotnet test` passes
3. **Dashboard page (`/`):** Dashboard link is highlighted with accent color and right border
4. **High Alching page (`/HighAlching`):** Only the High Alching link is highlighted; Dashboard is NOT highlighted
5. **Any other page:** Only that page's link is highlighted
6. **Sidebar collapsed state:** Active indicator still visible (icon color change) when sidebar is collapsed
7. **Page refresh:** Active state persists correctly (server-rendered, no flash of wrong state)

## Risks

- **None significant.** This is the standard ASP.NET MVC pattern for active nav links. The change is purely cosmetic/navigational with no impact on data or business logic.
- **Minor:** If new controllers are added in the future, developers must remember to add the `@(currentController == "ControllerName" ? "active" : "")` pattern to new nav links. This is standard practice and self-documenting in the Razor template.

## TODO

- [ ] Update `_Layout.cshtml` with server-side active class logic
- [ ] Remove client-side active-link code from `sidebar.js`
- [ ] Build and verify 0 errors
- [ ] Run tests and verify all pass
- [ ] Manual verification on Dashboard, High Alching, and at least one other page
