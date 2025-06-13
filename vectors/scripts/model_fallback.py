# vectors/scripts/model_fallback.py

import openai

# Preferred model order
FALLBACK_MODELS = [
    "gpt-4.5",           # research preview
    "gpt-4.1-mini",
    "o4-mini-high",
    "o4-mini",
    "o3"                 # GPT-3.5 Turbo
]

# Task → primary model mapping
MODEL_MAP = {
    "business_strategy": "gpt-4.5",
    "architecture_review": "gpt-4.5",
    "code_analysis": "gpt-4.1-mini",
    "vector_query": "o3",
    "bulk_processing": "o3",
}

def select_model(task_type: str) -> str:
    """
    Select the primary model for a given task type.
    Falls back to 'gpt-4.1-mini' if task_type is unrecognized.
    """
    return MODEL_MAP.get(task_type, "gpt-4.1-mini")

def query_with_model_fallback(query: str, task_type: str = "code_analysis"):
    """
    Query OpenAI ChatCompletion with automatic fallback across models.
    Attempts models in the order defined by FALLBACK_MODELS.
    """
    primary = select_model(task_type)
    start_index = FALLBACK_MODELS.index(primary) if primary in FALLBACK_MODELS else 0

    for model in FALLBACK_MODELS[start_index:]:
        try:
            response = openai.ChatCompletion.create(
                model=model,
                messages=[{"role": "user", "content": query}]
            )
            return response.choices[0].message.content
        except Exception as e:
            print(f"⚠️ Error with {model}: {e}, falling back...")

    raise RuntimeError("All models unavailable or failed.")
