import { getDocument, GlobalWorkerOptions, version } from "pdfjs-dist";
import * as mammoth from "mammoth";
import * as XLSX from "xlsx";
import { strFromU8, unzipSync } from "fflate";
import { toMarkdownLinkSmart } from "./markdown";
import { marked } from "marked";

// Set up the PDF.js worker source
GlobalWorkerOptions.workerSrc = `//cdn.jsdelivr.net/npm/pdfjs-dist@${version}/build/pdf.worker.mjs`;

/**
 * Converts a PDF File to plain text.
 * @param file PDF File object
 * @returns Promise<string> containing the extracted text
 */
export const pdfFileToText = async (file: File): Promise<string> => {
  const arrayBuffer = await file.arrayBuffer();
  const pdf = await getDocument({ data: arrayBuffer }).promise;
  let text = "";

  for (let pageNum = 1; pageNum <= pdf.numPages; pageNum++) {
    const page = await pdf.getPage(pageNum);
    const content = await page.getTextContent();
    text += content.items.map((item: any) => item.str).join(" ") + "\n";
  }

  return text;
};

/**
 * Extracts all files from a ZIP File using fflate.
 * @param file ZIP File object
 * @returns Promise<Record<string, Uint8Array>> with filenames as keys and file contents as Uint8Array
 */

export const zipFileToFiles = async (
  file: File
): Promise<Record<string, Uint8Array>> => {
  const arrayBuffer = await file.arrayBuffer();
  const files = unzipSync(new Uint8Array(arrayBuffer));
  return files;
};

/**
 * Converts a DOCX File to plain text using Mammoth.
 * @param file DOCX File object
 * @returns Promise<string> containing the extracted plain text
 */
export const docxFileToText = async (file: File): Promise<string> => {
  const arrayBuffer = await file.arrayBuffer();
  const { value } = await mammoth.extractRawText({ arrayBuffer });
  return value;
};

/**
 * Converts an Excel File (XLSX/XLS/CSV) to plain text (CSV).
 * @param file Excel File object
 * @returns Promise<string> containing the extracted text in CSV format
 */
export const excelFileToText = async (file: File): Promise<string> => {
  const arrayBuffer = await file.arrayBuffer();
  const workbook = XLSX.read(arrayBuffer, { type: "array" });

  const allText: string[] = workbook.SheetNames.map((sheetName) => {
    const sheet = workbook.Sheets[sheetName];
    const csv = XLSX.utils.sheet_to_csv(sheet);
    return `--- Sheet: ${sheetName} ---\n${csv.trim()}\n`;
  });

  return allText.join("\n");
};

/**
 * Converts a File to a Data URL string.
 * @param file File object
 * @returns Promise<string> Data URL
 */
export const fileToDataUrl = (file: File): Promise<string> =>
  new Promise<string>((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result as string);
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });


/**
 * Converts a PPTX PowerPoint file to plain text by extracting all text from slides.
 * @param file PPTX File object
 * @returns Promise<string> containing all slide text
 */
export const pptxFileToText = async (file: File): Promise<string> => {
  const arrayBuffer = await file.arrayBuffer();
  const files = unzipSync(new Uint8Array(arrayBuffer));
  let text = "";

  // Vind alle slides: 'ppt/slides/slide1.xml', etc.
  const slideFiles = Object.entries(files).filter(([filename]) =>
    /^ppt\/slides\/slide\d+\.xml$/i.test(filename)
  );

  for (const [filename, data] of slideFiles) {
    const xmlString = strFromU8(data);
    // <a:t> bevat tekstfragmenten in een slide
    const matches = Array.from(xmlString.matchAll(/<a:t>(.*?)<\/a:t>/g));
    for (const match of matches) {
      text += match[1].trim() + "\n";
    }
    text += "\n"; // scheiding tussen slides
  }

  return text.trim();
};

export const epubFileToTextBrute = async (file: File): Promise<string> => {
  const arrayBuffer = await file.arrayBuffer();
  const files = unzipSync(new Uint8Array(arrayBuffer));
  let text = "";

  // Optionally: parse toc.ncx or content.opf for real order
  // But here: just process all HTML-ish files
  for (const [filename, data] of Object.entries(files)) {
    if (/\.(xhtml|html|htm)$/i.test(filename)) {
      const htmlString = strFromU8(data);
      const doc = new DOMParser().parseFromString(htmlString, "text/html");
      // Pak alle zichtbare tekst uit <p>, <div>, <h1>...<h6>
      const tags = ["p", "div", "span", "h1", "h2", "h3", "h4", "h5", "h6"];
      for (const tag of tags) {
        doc.querySelectorAll(tag).forEach((node) => {
          const t = node.textContent?.trim();
          if (t) text += t + "\n";
        });
      }
    }
  }
  return text.trim();
};


// Utility to extract text from supported file types
export const extractTextFromFile = async (a: any): Promise<string | undefined> => {
  const file = a.file as File;
  const name = a.name || file.name || "";
  const type = a.type || file.type || "";

  // Prefer mimetype, fallback to extension
  if (type === "application/pdf" || /\.pdf$/i.test(name)) {
    return await pdfFileToText(file);
  }
  if (
    type === "application/vnd.openxmlformats-officedocument.wordprocessingml.document" ||
    /\.docx$/i.test(name)
  ) {
    return await docxFileToText(file);
  }
  if (
    type === "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" ||
    type === "application/vnd.ms-excel" ||
    /\.xlsx$/i.test(name) || /\.xls$/i.test(name) || /\.csv$/i.test(name)
  ) {
    return await excelFileToText(file);
  }
  if (
    type === "application/epub+zip" || /\.epub$/i.test(name)
  ) {
    return await epubFileToTextBrute(file);
  }

  if (
    type === "application/vnd.openxmlformats-officedocument.presentationml.presentation" ||
    /\.pptx$/i.test(name)
  ) {
    return await pptxFileToText(file); // this function uses PptxParser as shown previously
  }

  if (type === "text/plain" || /\.txt$/i.test(name)) {
    // Plain text file, just read as string
    return await file.text();
  }
  // Other: not supported, return undefined
  return undefined;
};


// Extracts all supported files in a ZIP and returns array of text parts
export const extractTextFromZip = async (a: any) => {
  const file = a.file as File;
  const files = await zipFileToFiles(file);
  const textParts: any[] = [];

  for (const [filename, data] of Object.entries(files)) {
    // Try to detect and extract text based on extension
    const ext = filename.split('.').pop()?.toLowerCase();
    const f = new File([data], filename);

    let text: string | undefined;
    if (ext === "pdf") text = await pdfFileToText(f);
    else if (ext === "docx") text = await docxFileToText(f);
    else if (["xlsx", "xls", "csv"].includes(ext || "")) text = await excelFileToText(f);
    else if (["txt", "md", "log"].includes(ext || "")) text = await f.text();

    if (text) {
      textParts.push({
        type: "text",
        text: toMarkdownLinkSmart(filename, text)
      });
    }
  }
  return textParts;
};

export function downloadBase64Image(base64Data: string, filename: string = "image.png") {
  // Create a link element
  const link = document.createElement("a");
  // Set the download attribute with the desired filename
  link.download = filename;
  // Set the href to the base64 image data
  link.href = base64Data;
  // Append, click, and remove the link
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

export const copyMarkdownToClipboard = async (markdown: string) => {
  if (markdown) {
    const html = await marked(markdown);
    const blob = new Blob([html], { type: "text/html" });
    const textBlob = new Blob([markdown], { type: "text/plain" });
    const data = [
      new ClipboardItem({ "text/html": blob, "text/plain": textBlob }),
    ];
    await navigator.clipboard.write(data);
  }
}