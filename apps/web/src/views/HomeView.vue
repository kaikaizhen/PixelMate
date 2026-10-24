<script setup lang="ts">
import { useHead } from '@unhead/vue'
import HeroSection from '@/components/business/HeroSection.vue'
import fontPixel from '@fontsource/press-start-2p/files/press-start-2p-latin-400-normal.woff2?url'
import roomImg from '@/assets/pixel/room.png'
import petIdleImg from '@/assets/pixel/pet_idle.png'
import DailySection from '@/components/business/DailySection.vue'
import MailSection from '@/components/business/MailSection.vue'
import DressSection from '@/components/business/DressSection.vue'
import WeatherSection from '@/components/business/WeatherSection.vue'
import PlatformsSection from '@/components/business/PlatformsSection.vue'
import ComingSoonSection from '@/components/business/ComingSoonSection.vue'

const TITLE = '小伴星PixelMate｜8-bit 像素虛擬寵物，即將與你見面'
const DESCRIPTION =
  '小伴星PixelMate 是一款 8-bit 像素風虛擬寵物：牠在小房間裡過自己的日子，能換帽子、圍巾與藍白拖，也會替你把遠方的信送到身邊。即將推出手機 PWA，之後加入桌面小螢幕。'
const siteUrl = (import.meta.env.VITE_SITE_URL as string | undefined)?.replace(/\/$/, '')

const jsonLd = {
  '@context': 'https://schema.org',
  '@type': 'WebSite',
  name: '小伴星PixelMate',
  alternateName: 'PixelMate',
  description: DESCRIPTION,
  inLanguage: 'zh-Hant',
  ...(siteUrl ? { url: `${siteUrl}/` } : {}),
}

useHead({
  htmlAttrs: { lang: 'zh-Hant' },
  title: TITLE,
  script: [{ type: 'application/ld+json', innerHTML: JSON.stringify(jsonLd) }],
  meta: [
    { name: 'description', content: DESCRIPTION },
    { name: 'google-site-verification', content: '_WoQRI7EiUJTqIH-UaxEPAWMMpzPRHCIM5J-cE4KwQ8' },
    { name: 'robots', content: 'index, follow, max-image-preview:large' },
    { property: 'og:type', content: 'website' },
    { property: 'og:locale', content: 'zh_TW' },
    { property: 'og:title', content: TITLE },
    { property: 'og:description', content: DESCRIPTION },
    { name: 'twitter:card', content: 'summary_large_image' },
    { name: 'twitter:title', content: TITLE },
    { name: 'twitter:description', content: DESCRIPTION },
    { property: 'og:site_name', content: '小伴星PixelMate' },
    { property: 'og:image:type', content: 'image/png' },
    { property: 'og:image:width', content: '1200' },
    { property: 'og:image:height', content: '630' },
    { property: 'og:image:alt', content: '一隻像素小夥伴待在溫暖的小房間裡，旁邊寫著「有些想念，會自己走到你身邊」' },
    ...(siteUrl
      ? [
          { property: 'og:url', content: `${siteUrl}/` },
          { property: 'og:image', content: `${siteUrl}/og-image.png` },
          { name: 'twitter:image', content: `${siteUrl}/og-image.png` },
        ]
      : []),
  ],
  link: [
    // 首屏必要的像素圖提早載入
    { rel: 'preload', as: 'image', href: roomImg, fetchpriority: 'high' },
    { rel: 'preload', as: 'image', href: petIdleImg },
    { rel: 'preload', as: 'font', type: 'font/woff2', href: fontPixel, crossorigin: 'anonymous' },
    ...(siteUrl ? [{ rel: 'canonical', href: `${siteUrl}/` }] : []),
  ],
})
</script>

<template>
  <HeroSection />
  <DailySection />
  <MailSection />
  <DressSection />
  <WeatherSection />
  <PlatformsSection />
  <ComingSoonSection />
</template>
