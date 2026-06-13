<script lang="ts">
	import { onMount } from 'svelte';

	type User = {
		id: number;
		discordId: string;
		username: string;
		avatarUrl: string | null;
		role: string;
	};

	let user = $state<User | null>(null);
	let loading = $state(true);
	let error = $state('');

	onMount(async () => {
		try {
			const response = await fetch('/api/auth/me');

			if (!response.ok) {
				throw new Error(`Authentication check failed: ${response.status}`);
			}

			user = await response.json();
		} catch (err) {
			console.error(err);
			error = 'Could not load the logged-in user.';
		} finally {
			loading = false;
		}
	});
</script>

<main>
	{#if loading}
		<p>Loading your profile...</p>
	{:else if error}
		<h1>Login failed</h1>
		<p>{error}</p>
		<a href="/">Return to login</a>
	{:else if user}
		<h1>Login successful</h1>

		{#if user.avatarUrl}
			<img
				src={user.avatarUrl}
				alt={`${user.username}'s Discord avatar`}
				width="96"
				height="96"
			/>
		{/if}

		<p>Welcome, {user.username}!</p>
		<p>Your local user ID is: {user.id}</p>
		<p>Role: {user.role}</p>

		<a href="/">Go to dashboard</a>
	{/if}
</main>