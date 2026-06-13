import adapter from '@sveltejs/adapter-auto';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],

	server: {
		proxy: {
			'/api': {
				target: 'http://localhost:5078',
				changeOrigin: true,
				secure: false
			}
		}
	}
});
