import { useState } from "react";
import NewsFeed from "./components/NewsFeed";
import Login from "./components/Login";
import Preferences from "./components/Preferences";
import { getToken, getUser } from "./lib/auth";
import Header from "./components/Header";
import "./App.css";

export default function App() {
  const [user, setUser] = useState(getUser());
  const token = getToken();
  const [reloadKey, setReloadKey] = useState(0);

  if (!token) return <Login onLoggedIn={u => setUser(u)} />;

   return (
    <>
      <Header title="My Epic News Feed" />
      <main style={{ width: "min(1100px, 92vw)", margin: "24px auto" }}>
        <Preferences onChanged={() => setReloadKey((k) => k + 1)} />
        <NewsFeed refreshKey={reloadKey} />
      </main>
    </>
  );
}
