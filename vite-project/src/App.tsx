import { useState } from "react";
import NewsFeed from "./components/NewsFeed";
import Login from "./components/Login";
import Preferences from "./components/Preferences";
import { getToken, getUser, logout } from "./lib/auth";
import Header from "./components/Header";
import "./App.css";

export default function App() {
  const [_user, setUser] = useState(getUser());
  const token = getToken();

  if (!token) return <Login onLoggedIn={u => setUser(u)} />;

    return (
    <>
      <Header 
  title="My Epic News Feed" 
  onLogout={() => { 
    logout();
    setUser(null as any);
  }} 
/>
      <main style={{ width: "min(1100px, 92vw)", margin: "24px auto" }}>
        {/* Preferences will invalidate queries directly */}
        <Preferences />
        <NewsFeed />
      </main>
    </>
  );
}
