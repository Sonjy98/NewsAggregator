export type SortMode = "recent" | "score" | "mixed";

export type PreferencesDto = {
  keywords: string[];
  excludedKeywords: string[];
  timeWindowDays: number | null;
  languages: string[];
  categories: string[];
  sort: SortMode;
};
