import type { Article } from "../types/Article";

interface Props {
    article: Article;
}

export default function NewsCard({ article }: Props) {
    return (
        <article className="news-card">
            <h2>{article.title}</h2>
            <div className="meta">
                {new Date(article.publishedAt).toLocaleString()} • {article.author}
            </div>
            <p>{article.image && (
                <img
                    src={article.image}
                    alt={article.title}
                    style={{ width: "100%", borderRadius: "8px", marginBottom: "1rem" }}
                />
            )}</p>
            <p>{article.body}</p>
        </article>
    );
}
