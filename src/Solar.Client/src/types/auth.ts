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
