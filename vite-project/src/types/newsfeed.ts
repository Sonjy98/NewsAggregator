export interface NewsItem {
  id: number;
  title: string;
  source: string;
  url: string;
  publishedAt: string;
  summary?: string;
}

export interface Keyword {
  id: number;
  word: string;
  createdAt?: string;
}

export interface UserPreferences {
  id: number;
  userId: string;
  keywords: Keyword[];
  mode: string;
}
