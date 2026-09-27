import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate } from 'react-router'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { AuthLayout, FormErrors } from './AuthLayout'
import { signUp } from './authApi'
import { useSignedIn } from './useSignedIn'
import { formText } from '../lib/formData'

export function SignUpPage() {
  const signedIn = useSignedIn()
  const navigate = useNavigate()
  const [errors, setErrors] = useState<string[]>([])
  const [pending, setPending] = useState(false)

  if (signedIn) return <Navigate to="/" replace />

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setPending(true)
    const result = await signUp(formText(form, 'email'), formText(form, 'password'))
    setPending(false)
    if (result.ok) await navigate('/workspaces/new', { replace: true })
    else setErrors(result.messages)
  }

  return (
    <AuthLayout title="Create your account">
      <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
        <FormErrors messages={errors} />
        <Input label="Email" name="email" type="email" autoComplete="email" required />
        <Input
          label="Password"
          name="password"
          type="password"
          autoComplete="new-password"
          hint="At least 6 characters with upper and lower case letters, a digit, and a symbol."
          required
        />
        <Button type="submit" variant="primary" className="w-full" disabled={pending}>
          {pending ? 'Creating account' : 'Create account'}
        </Button>
        <p className="text-center text-sm text-zinc-500">
          Already have an account?{' '}
          <Link to="/sign-in" className="font-medium text-indigo-600 dark:text-indigo-400">
            Sign in
          </Link>
        </p>
      </form>
    </AuthLayout>
  )
}
