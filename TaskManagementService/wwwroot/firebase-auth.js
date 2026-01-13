let app;
let auth;

window.firebaseAuth = {
    init: function () {
        if (app) return;

        const firebaseConfig = {
            apiKey: "AIzaSyDrrX1cqZz9N2_SB707w-2Rgfp6LwdFjBc",
            authDomain: "taskmanagementservice-ce728.firebaseapp.com",
            projectId: "taskmanagementservice-ce728",
            storageBucket: "taskmanagementservice-ce728.firebasestorage.app",
            messagingSenderId: "268889077298",
            appId: "1:268889077298:web:b7092019d6b2ed9b7c2e2b"
        };

        app = firebase.initializeApp(firebaseConfig);
        auth = firebase.auth();
    },

    login: async function (email, password) {
        this.init();
        const cred = await auth.signInWithEmailAndPassword(email, password);
        return await cred.user.getIdToken();
    },

    register: async function (email, password) {
        this.init();
        const cred = await auth.createUserWithEmailAndPassword(email, password);
        return await cred.user.getIdToken();
    },

    logout: async function () {
        this.init();
        await auth.signOut();
    },

    getIdToken: async function () {
        this.init();
        const user = auth.currentUser;
        if (!user) return null;
        return await user.getIdToken();
    }
};
