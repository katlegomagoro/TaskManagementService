console.log('firebase-auth.js loading...');

let app;
let auth;

window.firebaseAuth = {
    init: function () {
        console.log('firebaseAuth.init called');
        if (app) {
            console.log('Firebase app already initialized');
            return app;
        }

        console.log('Initializing Firebase...');
        const firebaseConfig = {
            apiKey: "AIzaSyDrrX1cqZz9N2_SB707w-2Rgfp6LwdFjBc",
            authDomain: "taskmanagementservice-ce728.firebaseapp.com",
            projectId: "taskmanagementservice-ce728",
            storageBucket: "taskmanagementservice-ce728.firebasestorage.app",
            messagingSenderId: "268889077298",
            appId: "1:268889077298:web:b7092019d6b2ed9b7c2e2b"
        };

        try {
            app = firebase.initializeApp(firebaseConfig);
            auth = firebase.auth();
            console.log('Firebase initialized successfully');
            return app;
        } catch (error) {
            console.error('Error initializing Firebase:', error);
            throw error;
        }
    },

    login: async function (email, password) {
        console.log('firebaseAuth.login called with:', email);
        this.init();
        try {
            const cred = await auth.signInWithEmailAndPassword(email, password);
            const token = await cred.user.getIdToken();
            console.log('Login successful, token received');
            return token;
        } catch (error) {
            console.error('Login error:', error);
            throw error;
        }
    },

    register: async function (email, password) {
        console.log('firebaseAuth.register called with:', email);
        this.init();
        try {
            const cred = await auth.createUserWithEmailAndPassword(email, password);
            const token = await cred.user.getIdToken();
            console.log('Registration successful, token received');
            return token;
        } catch (error) {
            console.error('Registration error:', error);
            throw error;
        }
    },

    logout: async function () {
        console.log('firebaseAuth.logout called');
        this.init();
        try {
            await auth.signOut();
            console.log('Logout successful');
        } catch (error) {
            console.error('Logout error:', error);
            throw error;
        }
    },

    getIdToken: async function () {
        console.log('firebaseAuth.getIdToken called');
        this.init();
        const user = auth.currentUser;
        if (!user) {
            console.log('No user currently signed in');
            return null;
        }
        const token = await user.getIdToken();
        console.log('ID token retrieved');
        return token;
    },

    getCurrentUser: function () {
        console.log('firebaseAuth.getCurrentUser called');
        this.init();
        const user = auth.currentUser;
        if (!user) {
            console.log('No current user');
            return null;
        }

        const userInfo = {
            uid: user.uid,
            email: user.email,
            displayName: user.displayName,
            photoURL: user.photoURL,
            emailVerified: user.emailVerified,
            metadata: {
                creationTime: user.metadata.creationTime,
                lastSignInTime: user.metadata.lastSignInTime
            }
        };
        console.log('Current user:', userInfo);
        return userInfo;
    }
};

console.log('firebaseAuth object created:', window.firebaseAuth);