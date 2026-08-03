<template>
  <div class="min-h-screen flex flex-col">
    <main class="page-wrap">
      <!-- fade transition between pages, out-in ensures old page exits before new one enters -->
      <router-view v-slot="{ Component }">
        <transition name="fade" mode="out-in">
          <component :is="Component" />
        </transition>
      </router-view>
    </main>
  </div>
</template>
