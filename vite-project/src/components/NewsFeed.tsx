import NewsCard from "./NewsCard";
import { useForMeNews } from "../hooks/useForMeNews";

export default function NewsFeed() {
  const { data, isLoading, error } = useForMeNews();

  if (isLoading) return <p>Loading news…</p>;
  if (error) return <p style={{ color: "crimson" }}>Error: {(error as Error).message}</p>;
  if (!data?.length) return <p>No articles yet. Add some keywords above.</p>;

  return <div>{data.map(a => <NewsCard key={a.id} article={a} />)}</div>;
}
