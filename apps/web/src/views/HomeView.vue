<script setup lang="ts">
import { useHead } from '@unhead/vue'
import HeroSection from '@/components/business/HeroSection.vue'
import DailySection from '@/components/business/DailySection.vue'
import MailSection from '@/components/business/MailSection.vue'

const TITLE = '小伴星PixelMate｜即將與你見面'
const DESCRIPTION =
  '小伴星PixelMate 是一隻會陪你生活、陪你成長，也會幫你把心意送到遠方的像素小夥伴。即將與你見面。'
const siteUrl = (import.meta.env.VITE_SITE_URL as string | undefined)?.replace(/\/$/, '')

useHead({
  title: TITLE,
  meta: [
    { name: 'description', content: DESCRIPTION },
    { property: 'og:type', content: 'website' },
    { property: 'og:locale', content: 'zh_TW' },
    { property: 'og:title', content: TITLE },
    { property: 'og:description', content: DESCRIPTION },
    { name: 'twitter:card', content: 'summary_large_image' },
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
</template>
