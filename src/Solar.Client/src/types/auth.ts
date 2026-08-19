export interface UserProfile {
  id: number;
  username: string;
  name: string;
  email: string;
  cpf?: string;
  profileTypes: number;
}

export interface LoginResponse {
  success: boolean;
  message?: string;
  token?: string;
  user?: UserProfile;
  passwordUpgraded: boolean;
}

export interface VerifyCpfResponse {
  existsInLocal: boolean;
  existsInSigaa: boolean;
  name?: string;
  email?: string;
  message: string;
}

export const isAdminUser = (user: UserProfile | null | undefined): boolean => {
  if (!user) return false;
  return (user.profileTypes & 16) === 16 || user.username?.toLowerCase() === 'admin';
};

export const isTeacherUser = (user: UserProfile | null | undefined): boolean => {
  if (!user) return false;
  return (user.profileTypes & 4) === 4 || user.username?.toLowerCase().startsWith('prof');
};
