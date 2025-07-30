import type { Article } from "../types/Article";
import data from "../data/articles.json";
import NewsCard from "./NewsCard";

export default function NewsFeed() {
  return (
    <div>
      {data.map((article: Article) => (
        <NewsCard key={article.id} article={article} />
      ))}
    </div>
  );
}
