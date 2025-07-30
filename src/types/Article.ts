// Using interface because we expect this shape to be *implemented* elsewhere.
export interface Article {
  id: string;           // Primary key – good habit even for mock data
  title: string;
  body: string;
  author: string;
  publishedAt: string;  // ISO date string – later you might convert to Date
  image?: string;       // Optional prop (note the ?)
}
