export type AuthUser = {
	id: number;
	discordId: string;
	username: string;
	avatarUrl: string | null;
	role: string;
};

export async function getCurrentUser(): Promise<AuthUser | null> {
	const response = await fetch('/api/auth/me', {
		credentials: 'include'
	});

	if (response.status === 401) {
		return null;
	}

	if (!response.ok) {
		throw new Error(`Could not load authenticated user: ${response.status}`);
	}

	return response.json();
}

export async function logout(): Promise<void> {
	const response = await fetch('/api/auth/logout', {
		method: 'POST',
		credentials: 'include'
	});

	if (!response.ok) {
		throw new Error(`Logout failed: ${response.status}`);
	}
}