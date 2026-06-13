<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { getCurrentUser, logout, type AuthUser } from '$lib/auth';

	let user = $state<AuthUser | null>(null);
	let loading = $state(true);
	let error = $state('');
	let loggingOut = $state(false);

	onMount(async () => {
		try {
			user = await getCurrentUser();

			if (!user) {
				await goto('/');
				return;
			}
		} catch (err) {
			console.error(err);
			error = 'Could not load your account.';
		} finally {
			loading = false;
		}
	});

	async function handleLogout() {
		loggingOut = true;
		error = '';

		try {
			await logout();
			await goto('/');
		} catch (err) {
			console.error(err);
			error = 'Could not log out.';
			loggingOut = false;
		}
	}
</script>

<svelte:head>
	<title>Dashboard | Dojo League</title>
</svelte:head>

<main>
	{#if loading}
		<p>Loading dashboard...</p>
	{:else if error}
		<h1>Something went wrong</h1>
		<p>{error}</p>
	{:else if user}
		<section class="profile">
			{#if user.avatarUrl}
				<img
					src={user.avatarUrl}
					alt={`${user.username}'s Discord avatar`}
					width="96"
					height="96"
				/>
			{/if}

			<div>
				<h1>Welcome, {user.username}</h1>
				<p>Role: {user.role}</p>
				<p>User ID: {user.id}</p>
			</div>
		</section>

		<section class="dashboard-content">
			<h2>Your dashboard</h2>
			<p>Your tournaments, matches and rankings will appear here.</p>
		</section>

		<button onclick={handleLogout} disabled={loggingOut}>
			{loggingOut ? 'Logging out...' : 'Log out'}
		</button>
	{/if}
</main>

<style>
	main {
		max-width: 800px;
		margin: 0 auto;
		padding: 3rem 1.5rem;
	}

	.profile {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	img {
		border-radius: 50%;
	}

	.dashboard-content {
		margin: 3rem 0;
		padding: 1.5rem;
		border: 1px solid #ccc;
		border-radius: 0.75rem;
	}

	button {
		padding: 0.75rem 1.25rem;
		border: 0;
		border-radius: 0.5rem;
		cursor: pointer;
	}

	button:disabled {
		cursor: not-allowed;
		opacity: 0.6;
	}
</style>