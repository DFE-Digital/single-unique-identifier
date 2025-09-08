#!/usr/bin/env bash

# Exit immediately if a command exits with a non-zero status.
set -e

# --- Configuration ---
# The main markdown file to update.
OVERVIEW_FILE="overview.md"
# The top-level directories to scan for documentation.
DIRS_TO_SCAN=("System" "Component")


# --- Function Definitions ---

# Recursively scans a directory to generate markdown tables for any .md files found.
#
# @param {string} dir_path The path to the directory to scan.
# @param {string} heading_level The markdown heading level to use (e.g., "###").
# @param {string} output_file The temporary file to write the generated markdown to.
generate_tables_for_dir() {
    local dir_path="$1"
    local heading_level="$2"
    local output_file="$3"

    # Check if there are any markdown files directly within this directory.
    # We use a loop and break to efficiently check for the existence of at least one file.
    local md_files_found=false
    while IFS= read -r -d $'\0' md_file; do
        md_files_found=true
        break
    done < <(find "$dir_path" -maxdepth 1 -type f -name "*.md" -print0)

    local dir_name
    dir_name=$(basename "$dir_path")

    # If markdown files were found, generate a table for this directory.
    if [ "$md_files_found" = true ]; then
        # Append the heading and table header to the output file.
        echo "" >> "$output_file"
        echo "$heading_level $dir_name" >> "$output_file"
        echo "" >> "$output_file"
        echo "| ID | Name |" >> "$output_file"
        echo "|----|------|" >> "$output_file"

        # Process each markdown file to create a table row.
        # The '-print0' and 'sort -z' options handle filenames with spaces or special characters.
        while IFS= read -r -d $'\0' md_file; do
            # Read only the first line of the file. Continue if the file is empty.
            read -r first_line < "$md_file" || continue

            # Use bash regular expression to extract the ID and Name from the heading.
            # The format expected is: '# <ID>: <Name>'
            if [[ $first_line =~ ^#\ ([^:]+):\ (.*)$ ]]; then
                local id="${BASH_REMATCH[1]}"
                local name="${BASH_REMATCH[2]}"
                # Create a relative markdown link.
                local link="[${id}](${md_file})"

                # Append the formatted table row to the output file.
                echo "| ${link} | ${name} |" >> "$output_file"
            fi
        done < <(find "$dir_path" -maxdepth 1 -type f -name "*.md" -print0 | sort -z)
    fi

    # --- Recursion Step ---
    # Now, find all subdirectories and call this function again for each one.
    local next_heading_level="#$heading_level"
    while IFS= read -r -d $'\0' sub_dir; do
        echo "" >> "$output_file"
        echo "$heading_level $dir_name" >> "$output_file"
    
        generate_tables_for_dir "$sub_dir" "$next_heading_level" "$output_file"
    done < <(find "$dir_path" -maxdepth 1 -mindepth 1 -type d -print0 | sort -z)
}


# --- Main Script Logic ---

# 1. Validate that the overview file exists and contains the "## Summary" heading.
if [ ! -f "$OVERVIEW_FILE" ]; then
    echo "Error: The file '$OVERVIEW_FILE' was not found." >&2
    exit 1
fi

summary_line_num=$(grep -n -m 1 "## Summary of ADRs" "$OVERVIEW_FILE" | cut -d: -f1)

if [ -z "$summary_line_num" ]; then
    echo "Error: Heading '## Summary' not found in '$OVERVIEW_FILE'." >&2
    exit 1
fi

# 2. Create a temporary file to hold the new table content.
# 'trap' ensures the temp file is deleted when the script exits, even on error.
TEMP_TABLES=$(mktemp)
trap 'rm -f -- "$TEMP_TABLES"' EXIT

# 3. Generate the markdown content by scanning the specified directories.
for dir in "${DIRS_TO_SCAN[@]}"; do
    if [ -d "$dir" ]; then
        generate_tables_for_dir "$dir" "###" "$TEMP_TABLES"
    fi
done

# 4. To ensure idempotency, we rebuild the overview file from scratch.
# First, create another temporary file for the new overview content.
TEMP_OVERVIEW=$(mktemp)
trap 'rm -f -- "$TEMP_OVERVIEW"' EXIT

# Copy the content of the original file up to and including the "## Summary" line.
# This effectively deletes any previously generated table.
head -n "$summary_line_num" "$OVERVIEW_FILE" > "$TEMP_OVERVIEW"

# 5. Append the newly generated tables to the temporary overview file.
cat "$TEMP_TABLES" >> "$TEMP_OVERVIEW"

# 6. Overwrite the original overview file with the updated content.
mv "$TEMP_OVERVIEW" "$OVERVIEW_FILE"

echo "Successfully updated the summary table in '$OVERVIEW_FILE'."
