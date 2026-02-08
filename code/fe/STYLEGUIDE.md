# Witnes Design System - Style Guide

## Button Components

### Overview
We have a unified button system with consistent styling across the application. Use these components instead of inline styles.

### Button Variants

#### 1. Primary Button (CTA)
**Component:** `<Button variant="primary" />`
- Black background, white text
- Green hover state (#7A9B79)
- Rounded corners (xl)
- Shadow
- **Use for:** Main call-to-actions like "Start Tracking", "Log in"

**Example:**
```astro
<Button text="Start Tracking" href="/dashboard" variant="primary" size="md" />
```

**React:**
```tsx
import { Button } from "../components/landing/Button";
<Button text="Start Tracking" href="/dashboard" variant="primary" size="md" />
```

#### 2. Outline Button
**Component:** `<Button variant="outline" />`
- Border only (black/10)
- Zinc text color
- Green border + black text on hover
- **Use for:** Secondary actions like "Sign in as Demo"

**Example:**
```astro
<Button text="Sign in as Demo" href="#" variant="outline" size="md" />
```

#### 3. Ghost Button
**Component:** `<Button variant="ghost" />`
- No background or border
- Black text
- Opacity hover effect
- **Use for:** Tertiary actions, nav links

#### 4. Navbar Button
**Component:** `<Button variant="navbar" />`
- Border with current color
- Inverted on hover (black bg, white text)
- No rounded corners
- **Use for:** Navigation bar login/signup buttons

#### 5. Directional Button
**Component:** `<DirectionalButton />`
- Arrow icon (left or right)
- Black text with green hover (#99B898)
- Text translates with arrow on hover
- **Use for:** Navigation links like "See Example Report", "Back to home"

**Example:**
```astro
<DirectionalButton text="See Example Report" href="/example-report" direction="right" />
<DirectionalButton text="Back to home" href="/" direction="left" />
```

**React:**
```tsx
import { DirectionalButton } from "../components/landing/DirectionalButton";
<DirectionalButton text="Back to home" href="/" direction="left" />
```

### Button Sizes

- `size="sm"` - Small (px-8 py-4, text-[10px])
- `size="md"` - Medium (px-12 md:px-16 py-6 md:py-8) - **Default**
- `size="lg"` - Large (px-16 md:px-20 py-8 md:py-10)

### Usage Guidelines

**DO:**
- Use `<Button />` or `<DirectionalButton />` components
- Choose variant based on action hierarchy
- Keep button text short and action-oriented

**DON'T:**
- Don't write inline styles for buttons
- Don't create custom button classes
- Don't mix button styles from different variants

---

## Typography System

### Text Component

Use the `<Text />` component for all typography to ensure consistency.

**Component:** `<Text />`

**Props:**
- `variant`: Style preset (see below)
- `as`: HTML tag (h1, h2, p, span, div) - defaults based on variant
- `color`: Text color override
- `className`: Additional classes (use sparingly)

### Text Variants

#### 1. Duna (Hero Display)
**Variant:** `variant="duna"`
**Default Tag:** `h1`
**Style:** Custom Duna font, massive size, uppercase
**Use for:** Main hero headlines only

#### 2. Display Heading
**Variant:** `variant="display"`
**Default Tag:** `h2`
**Style:** Large, uppercase, heavy weight
**Use for:** Section headings

#### 3. Lead Text
**Variant:** `variant="lead"`
**Default Tag:** `p`
**Style:** Large, light weight, tight leading
**Use for:** Introduction text, hero subtitles

#### 4. Body Text
**Variant:** `variant="body"`
**Default Tag:** `p`
**Style:** Standard readable text, medium weight
**Use for:** General content

#### 5. Label
**Variant:** `variant="label"`
**Default Tag:** `span`
**Style:** Small, uppercase, tracking-wide
**Use for:** Section labels, badges

**Example:**
```astro
<Text variant="display" color="black">
  CLEAR GROUND.
</Text>

<Text variant="lead" color="zinc-800">
  Carbon accounting defined by honesty.
</Text>
```

**React:**
```tsx
import { Text } from "../components/landing/Text";
<Text variant="body" color="zinc-600">Content goes here</Text>
```

### Color Palette

- **Primary Green:** `#7A9B79` (`color="accent"`)
- **Secondary Green:** `#99B898` (`color="accent-secondary"`)
- **Black:** `#0D0D0D` (`color="black"`)
- **Zinc:** `color="zinc-800"`, `color="zinc-600"`, etc.

---

## Component Checklist

✅ **Buttons** - Componentized
✅ **Typography** - Componentized (Text component created)
⏳ **Forms** - To be componentized
⏳ **Cards** - To be componentized
⏳ **Layouts** - To be componentized

---

## Migration Progress

### Pages Updated:
- ✅ Hero section (landing page) - Using Button & Text components
- ✅ Login page - Using Button & DirectionalButton components
- ✅ Reports section - Using DirectionalButton component
- ✅ Landing navbar - Using Button component
- ✅ CTA section (index.astro) - Using Button component

### Remaining Inline Styles to Componentize:

#### 1. Form Inputs
**Location:** LoginWrapper.tsx
**Recommendation:** Create `<Input />` component

#### 2. Form Labels
**Location:** LoginWrapper.tsx
**Recommendation:** Create `<Label />` component or use `<Text variant="label" />`

#### 3. Cards/Panels
**Location:** LoginWrapper.tsx
**Recommendation:** Create `<Card />` component

#### 4. Text Links
**Location:** LoginWrapper.tsx
**Recommendation:** Create `<TextLink />` component