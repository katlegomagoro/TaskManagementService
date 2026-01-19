// Firebase Admin functions (for searching users)
window.firebaseAdmin = {
    // Search Firebase users by email or display name
    searchUsers: async (searchTerm) => {
        try {
            const response = await fetch(`/api/firebase/users/search?term=${encodeURIComponent(searchTerm)}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (response.ok) {
                return await response.json();
            }
            return [];
        } catch (error) {
            console.error('Error searching Firebase users:', error);
            return [];
        }
    },

    // Get all Firebase users
    getAllUsers: async () => {
        try {
            const response = await fetch('/api/firebase/users', {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                }
            });

            if (response.ok) {
                return await response.json();
            }
            return [];
        } catch (error) {
            console.error('Error getting Firebase users:', error);
            return [];
        }
    }
};
