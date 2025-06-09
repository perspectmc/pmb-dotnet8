# vectors/scripts/test_model_fallback.py

from model_fallback import select_model, query_with_model_fallback, FALLBACK_MODELS

# 1) Verify the primary-model mapping
print("Primary model mapping:")
for task in ["business_strategy", "architecture_review", "code_analysis", "vector_query", "unknown"]:
    print(f"  {task:20} → {select_model(task)}")

# 2) Verify the fallback order starts in the right place
primary = select_model("code_analysis")
idx = FALLBACK_MODELS.index(primary)
print("\nFallback sequence for 'code_analysis':")
print("  " + ", ".join(FALLBACK_MODELS[idx:]))

# 3) Dry-run the fallback logic by monkey-patching OpenAI errors:
import openai
real_create = openai.ChatCompletion.create

def fake_create(*args, **kwargs):
    # always pretend GPT-4.5 is unavailable, succeed on next
    model = kwargs.get("model", "")
    if model == "gpt-4.5":
        raise Exception("no access")
    # return a dummy response object
    class DummyMessage:
        def __init__(self, content):
            self.content = content
    class DummyChoice:
        def __init__(self, message):
            self.message = message
    class DummyResponse:
        def __init__(self, text):
            self.choices = [DummyChoice(DummyMessage(text))]
    return DummyResponse("✅ Success on " + model)

openai.ChatCompletion.create = fake_create

print("\nTesting fallback for business_strategy:")
print("  " + query_with_model_fallback("test", "business_strategy"))

# Restore original create method
openai.ChatCompletion.create = real_create