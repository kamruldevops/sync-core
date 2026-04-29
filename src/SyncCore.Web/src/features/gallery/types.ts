export interface PhotoDto {
  publicId: string;
  fileName: string;
  takenAt: string;
  thumbUrl: string;
  previewUrl: string;
  isFavourite: boolean;
  deletedAt?: string;
}

export interface PhotoDetailDto {
  publicId: string;
  fileName: string;
  takenAt: string;
  uploadedAt: string;
  fileSizeBytes: number;
  contentType: string;
  previewUrl: string;
  originalUrl: string;
  isFavourite: boolean;
  albumIds: string[];
}

export interface PhotoPage {
  items: PhotoDto[];
  nextCursor?: string;
}
