<script lang="ts">
	import { page } from '$app/state';

	const userId = page.url.searchParams.get('userId');
	const username = page.url.searchParams.get('username');
	const avatarUrl = page.url.searchParams.get('avatarUrl');
</script>

<main>
	<h1>Login successful</h1>

	{#if avatarUrl}
		<img src={avatarUrl} alt="Discord avatar" width="96" height="96" />
	{/if}

	<p>Welcome, {username}!</p>
	<p>Your local user ID is: {userId}</p>

	<a href="/">Go to dashboard</a>
</main>