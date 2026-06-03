import { initializeApp } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-app.js";
import { getAuth } from "https://www.gstatic.com/firebasejs/10.8.0/firebase-auth.js";

const firebaseConfig = {
  apiKey: "AIzaSyC__KDr9zp3uz3b7Ibvdq3sV9AQfQ9zTqw",
  authDomain: "bookbridge-ce003.firebaseapp.com",
  projectId: "bookbridge-ce003",
  storageBucket: "bookbridge-ce003.firebasestorage.app",
  messagingSenderId: "23021388549",
  appId: "1:23021388549:web:1ec512d51e202b91cf6d6c",
  measurementId: "G-77JH9FRYEF"
};

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);

export { app, auth };
