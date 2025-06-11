import pdfplumber
import json
import re
from pathlib import Path

# Load the PDF
pdf_path = "aidocs/PaymentSched/Payment_Schedule-April_1_2025-Final.pdf"
output_dir = Path("aidocs/PaymentSched")

# Output containers
policy_chunks = []
assessment_rules = []
location_modifiers = []
provincial_rules = []
explanatory_codes = []
fee_code_table = []

# Helper to clean and chunk text
def chunk_text(text, page_num, section):
    chunks = [p.strip() for p in text.split("\n\n") if p.strip()]
    return [{"page": page_num, "section": section, "text": chunk} for chunk in chunks]

# Enhanced explanatory code extraction
def extract_explanatory_codes(text, page_num):
    codes = []
    lines = text.split("\n")
    current_code = None
    current_desc = []
    
    for line in lines:
        # Check if line starts with two-letter code
        if len(line) >= 3 and line[:2].isalpha() and line[2] == " ":
            # Save previous code
            if current_code and current_desc:
                codes.append({
                    "code": current_code,
                    "description": " ".join(current_desc).strip(),
                    "page": page_num
                })
            # Start new code
            current_code = line[:2].strip()
            current_desc = [line[3:].strip()]
        elif current_code and line.strip() and not line.startswith("April 1, 2025"):
            # Continue description
            current_desc.append(line.strip())
    
    # Don't forget last code
    if current_code and current_desc:
        codes.append({
            "code": current_code,
            "description": " ".join(current_desc).strip(),
            "page": page_num
        })
    
    return codes

# Enhanced fee code extraction with structure
def extract_fee_codes(text, page_num):
    # Detect section
    section_match = re.search(r'S E C T I O N\s+([A-Z])\s*[-–]\s*(.+)', text)
    section = section_match.group(1) if section_match else "Unknown"
    section_name = section_match.group(2).strip() if section_match else "Unknown"
    
    # Extract fee codes with pattern
    fee_codes = []
    pattern = r'(\d+[A-Z])\s+(.+?)\s+\$?([\d,]+\.?\d*)\s+\$?([\d,]+\.?\d*)\s+(\d+|By Report)\s*([HLMN]?)'
    
    for match in re.finditer(pattern, text):
        fee_codes.append({
            "code": match.group(1),
            "description": match.group(2).strip(),
            "gp_fee": match.group(3),
            "specialist_fee": match.group(4),
            "class": match.group(5),
            "anesthesia": match.group(6) or "",
            "page": page_num,
            "section": section,
            "section_name": section_name
        })
    
    # If no structured codes found, fall back to raw lines
    if not fee_codes:
        rows = [line.strip() for line in text.split("\n") if line.strip()]
        return [{"page": page_num, "section": section, "rows": rows}]
    
    return fee_codes

with pdfplumber.open(pdf_path) as pdf:
    total_pages = len(pdf.pages)
    print(f"Processing {total_pages} pages...")
    
    for i, page in enumerate(pdf.pages):
        page_num = i + 1
        text = page.extract_text() or ""

        if not text.strip():
            continue  # Skip blank pages

        # Progress indicator
        if page_num % 50 == 0:
            print(f"Processed {page_num}/{total_pages} pages...")

        if 7 <= page_num <= 14:
            policy_chunks += chunk_text(text, page_num, "Introduction")

        elif 15 <= page_num <= 23 or 32 <= page_num <= 40:
            policy_chunks += chunk_text(text, page_num, "Policy")

        elif 24 <= page_num <= 29:
            rows = [line for line in text.split("\n") if line.strip()]
            assessment_rules.append({"page": page_num, "rows": rows})

        elif 30 <= page_num <= 31:
            rows = [line for line in text.split("\n") if line.strip()]
            location_modifiers.append({"page": page_num, "modifiers": rows})

        elif 41 <= page_num <= 42:
            provincial_rules += chunk_text(text, page_num, "Provincial Rules")

        elif 43 <= page_num <= 68:
            # ENHANCED: Multi-line explanatory codes
            codes = extract_explanatory_codes(text, page_num)
            explanatory_codes.extend(codes)

        elif 70 <= page_num <= total_pages:  # FIXED: Go to end, not 130
            # ENHANCED: Structured fee code extraction
            fee_data = extract_fee_codes(text, page_num)
            fee_code_table.extend(fee_data)

print("Extraction complete!")

# Save outputs as markdown files
def write_markdown(data, filename, title):
    md_filename = filename.replace('.json', '.md')
    with open(output_dir / md_filename, "w", encoding='utf-8') as f:
        f.write(f"# {title}\n\n")
        
        if filename == "explanatory_codes.md":
            # Special formatting for explanatory codes
            f.write("Complete list of claim rejection and explanation codes.\n\n")
            for code_data in data:
                f.write(f"## {code_data['code']}\n\n")
                f.write(f"**Page:** {code_data['page']}\n\n")
                f.write(f"{code_data['description']}\n\n")
        
        elif filename == "fee_code_table.md":
            # Special formatting for fee codes
            f.write("Complete fee schedule by medical specialty sections.\n\n")
            current_section = None
            
            for fee in data:
                if 'code' in fee:
                    # Structured fee data
                    section = fee.get('section', 'Unknown')
                    if section != current_section:
                        section_name = fee.get('section_name', 'Unknown Section')
                        f.write(f"\n## Section {section} - {section_name}\n\n")
                        f.write("| Code | Description | GP Fee | Specialist Fee | Class | Page |\n")
                        f.write("|------|-------------|--------|----------------|-------|------|\n")
                        current_section = section
                    
                    f.write(f"| **{fee['code']}** | {fee['description']} | ${fee['gp_fee']} | ${fee['specialist_fee']} | {fee['class']} | {fee['page']} |\n")
                else:
                    # Raw data fallback
                    page = fee['page']
                    f.write(f"\n### Page {page}\n\n")
                    for row in fee.get('rows', []):
                        f.write(f"{row}\n\n")
        
        elif filename == "chunks_policy.md":
            # Special formatting for policy chunks
            f.write("Payment Schedule policies and billing rules.\n\n")
            current_section = None
            for chunk in data:
                section = chunk['section']
                if section != current_section:
                    f.write(f"\n## {section}\n\n")
                    current_section = section
                f.write(f"### Page {chunk['page']}\n\n")
                f.write(f"{chunk['text']}\n\n")
        
        else:
            # Generic JSON format for other files
            f.write("```json\n")
            f.write(json.dumps(data, indent=2))
            f.write("\n```\n")
    
    print(f"Saved {md_filename} with {len(data)} items")

# Create output directory if it doesn't exist
output_dir.mkdir(parents=True, exist_ok=True)

write_markdown(policy_chunks, "chunks_policy.json", "Payment Schedule Policy")
write_markdown(assessment_rules, "assessment_rules_table.json", "Assessment Rules Table")
write_markdown(location_modifiers, "location_modifiers.json", "Location Modifiers")
write_markdown(provincial_rules, "provincial_rules.json", "Provincial Rules")
write_markdown(explanatory_codes, "explanatory_codes.json", "Explanatory Codes (AA-ZZ)")
write_markdown(fee_code_table, "fee_code_table.json", "Fee Code Table")

print(f"\nSummary:")
print(f"Policy chunks: {len(policy_chunks)}")
print(f"Assessment rules: {len(assessment_rules)}")
print(f"Location modifiers: {len(location_modifiers)}")
print(f"Provincial rules: {len(provincial_rules)}")
print(f"Explanatory codes: {len(explanatory_codes)}")
print(f"Fee code entries: {len(fee_code_table)}")

# Check for ZA code specifically
za_codes = [code for code in explanatory_codes if code.get('code') == 'ZA']
if za_codes:
    print(f"\n✅ Found ZA code: {za_codes[0]['description']}")
else:
    print(f"\n❌ ZA code not found")