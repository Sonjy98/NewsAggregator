import { useState } from "react";
import NewsFeed from "./components/NewsFeed";
import Login from "./components/Login";
import Preferences from "./components/Preferences";
import { getToken, getUser, logout } from "./lib/auth";
import EmailMeButton from "./components/EmailMeButton";
import "./App.css";

export default function App() {
  const [user, setUser] = useState(getUser());
  const token = getToken();
  const [reloadKey, setReloadKey] = useState(0);

  if (!token) return <Login onLoggedIn={u => setUser(u)} />;

  return (
    <main>
      <header style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <h1>My Epic News Feed</h1>
        <EmailMeButton max={10} />
        <div>
          <span style={{ marginRight: 12 }}>{user?.email}</span>
          <button onClick={() => { logout(); location.reload(); }}>Log out</button>
        </div>
      </header>

      <Preferences onChanged={() => setReloadKey(k => k + 1)} />
      <NewsFeed refreshKey={reloadKey} />
    </main>
  );
}
