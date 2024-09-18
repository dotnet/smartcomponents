import * as transformers from 'https://cdn.jsdelivr.net/npm/@xenova/transformers/dist/transformers.min.js';

// Begin downloading the embedding model in the background
const pipePromise = transformers.pipeline('embeddings', 'TaylorAI/bge-micro-v2');

// Converts a string into a semantic vector
export async function embedText(text) {
    const pipe = await pipePromise; // Waits for the model to be downloaded etc.
    const result = await pipe(text, { pooling: 'mean', normalize: true });
    return result.data;
}

// Computes the similarity of two semantic vectors.
// You can make this faster with SIMD. Here's a simple algorithm for clarity.
export function cosineSimilarity(vec1, vec2) {
    let result = 0;
    for (let i = 0; i < vec1.length; i++) {
        result += vec1[i] * vec2[i];
    }
    return result;
}
