#!/usr/bin/env python3
"""
PDF text extractor for OPZManager.
Accepts a PDF file path as CLI argument, outputs extracted text as JSON to stdout.
Usage: python pdf_converter.py <path_to_pdf>
"""

import sys
import json

try:
    import fitz  # PyMuPDF
except ImportError:
    print(json.dumps({"error": "PyMuPDF not installed. Run: pip install PyMuPDF"}), file=sys.stdout)
    sys.exit(1)


def extract_text(pdf_path: str) -> dict:
    """Extract text and tables from a PDF file."""
    try:
        doc = fitz.open(pdf_path)
        pages = []

        for page_num in range(len(doc)):
            page = doc.load_page(page_num)
            text = page.get_text().strip()

            # Try table extraction
            tables_data = []
            try:
                tables = page.find_tables()
                for table in tables:
                    try:
                        df = table.extract()
                        if df and len(df) > 0:
                            tables_data.append(df)
                    except Exception:
                        continue
            except Exception:
                pass

            pages.append({
                "pageNumber": page_num + 1,
                "text": text,
                "tables": tables_data,
            })

        doc.close()

        full_text = "\n\n".join(p["text"] for p in pages if p["text"])

        return {
            "success": True,
            "totalPages": len(pages),
            "fullText": full_text,
            "pages": pages,
        }

    except Exception as e:
        return {
            "success": False,
            "error": str(e),
        }


def main():
    if len(sys.argv) < 2:
        print(json.dumps({"error": "Usage: python pdf_converter.py <path_to_pdf>"}))
        sys.exit(1)

    pdf_path = sys.argv[1]
    result = extract_text(pdf_path)
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
