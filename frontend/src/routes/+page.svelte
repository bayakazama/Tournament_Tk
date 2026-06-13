<script lang="ts">
	import { onMount } from 'svelte';
	import { getHealth } from '$lib/api';

	let status = $state('Loading...');
	let app = $state('');
	let error = $state('');

	onMount(async () => {
		try {
			const data = await getHealth();

			status = data.status;
			app = data.app;
		} catch (err) {
			error = 'Could not connect to backend';
			console.error(err);
		}
	});
</script>

<main>
	<h1>Dojo League</h1>

	{#if error}
		<p>{error}</p>
	{:else}
		<p>Backend status: {status}</p>
		<p>App: {app}</p>
	{/if}

	<a class="discord-login" href="/api/auth/discord/login">
		Login with Discord
	</a>
</main>

<style>
	main {
		max-width: 700px;
		margin: 0 auto;
		padding: 4rem 1.5rem;
		text-align: center;
	}

	.discord-login {
		display: inline-block;
		margin-top: 2rem;
		padding: 0.8rem 1.4rem;
		border-radius: 0.5rem;
		background: #5865f2;
		color: white;
		font-weight: 600;
		text-decoration: none;
	}

	.discord-login:hover {
		opacity: 0.9;
	}
</style>