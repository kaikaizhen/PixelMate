<script setup lang="ts">
import { useHead } from '@unhead/vue'
import HeroSection from '@/components/business/HeroSection.vue'
import DailySection from '@/components/business/DailySection.vue'
import MailSection from '@/components/business/MailSection.vue'
import DressSection from '@/components/business/DressSection.vue'
import WeatherSection from '@/components/business/WeatherSection.vue'
import PlatformsSection from '@/components/business/PlatformsSection.vue'
import ComingSoonSection from '@/components/business/ComingSoonSection.vue'

const TITLE = '小伴星PixelMate｜即將與你見面'
const DESCRIPTION =
  '小伴星PixelMate 是一隻會陪你生活、陪你成長，也會幫你把心意送到遠方的像素小夥伴。即將與你見面。'
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
    { property: 'og:type', content: 'website' },
    { property: 'og:locale', content: 'zh_TW' },
    { property: 'og:title', content: TITLE },
    { property: 'og:description', content: DESCRIPTION },
    { name: 'twitter:card', content: 'summary_large_image' },
    { name: 'twitter:title', content: TITLE },
    { name: 'twitter:description', content: DESCRIPTION },
    { property: 'og:site_name', content: '小伴星PixelMate' },
    { property: 'og:image:alt', content: '一隻像素小夥伴待在溫暖的小房間裡，旁邊寫著「有些想念，會自己走到你身邊」' },
    ...(siteUrl
      ? [
          { property: 'og:url', content: `${siteUrl}/` },
          { property: 'og:image', content: `${siteUrl}/og-image.png` },
          { name: 'twitter:image', content: `${siteUrl}/og-image.png` },
        ]
      : []),
  ],
  link: siteUrl ? [{ rel: 'canonical', href: `${siteUrl}/` }] : [],
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
